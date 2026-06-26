using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Admin;
using TenderDocs.Application.Features.Documents;

namespace TenderDocs.Application.Features.Projects;

public record GetProjectQuery(Guid Id) : IRequest<ProjectDetailDto>;

public class GetProjectHandler : IRequestHandler<GetProjectQuery, ProjectDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GetProjectHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<ProjectDetailDto> Handle(GetProjectQuery q, CancellationToken ct)
    {
        // Non-admins may only open a project they're assigned to (otherwise it's "not found" to them).
        var isAdmin = ProjectAccessScope.IsAdmin(_current);
        var p = await _db.Projects
            .AsNoTracking()   // read-only projection; also avoids just-soft-deleted rows leaking back via fixup
            .Include(x => x.RequirementCategories).ThenInclude(c => c.Requirements)
            .Include(x => x.Requirements)
            .Include(x => x.Assignments).ThenInclude(a => a.Document).ThenInclude(d => d.UploadedBy)
            .Include(x => x.Assignments).ThenInclude(a => a.Document).ThenInclude(d => d.DocumentTags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(x => x.Id == q.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted
                && (isAdmin || _db.UserProjects.Any(up => up.ProjectId == x.Id && up.UserId == _current.UserId)), ct)
            ?? throw new NotFoundException("Project", q.Id);

        var docs = p.Assignments.Where(a => !a.Document.IsDeleted)
            .Select(a => DocumentMapping.ToDto(a.Document)).ToList();

        ProjectRequirementDto ToReqDto(Domain.Entities.ProjectRequirement r) =>
            new(r.Id, r.Name, r.Description, r.IsMandatory, r.SortOrder, r.CategoryId,
                p.Assignments.FirstOrDefault(a => a.RequirementId == r.Id)?.DocumentId);

        var reqs = p.Requirements.OrderBy(r => r.SortOrder).Select(ToReqDto).ToList();

        var categories = p.RequirementCategories.OrderBy(c => c.SortOrder)
            .Select(c => new ProjectRequirementCategoryDto(c.Id, c.Name, c.SortOrder,
                c.Requirements.OrderBy(r => r.SortOrder).Select(ToReqDto).ToList()))
            .ToList();

        var assignments = p.Assignments.Where(a => !a.Document.IsDeleted)
            .Select(a => new ProjectAssignmentDto(a.DocumentId, a.RequirementId))
            .ToList();

        return new ProjectDetailDto(p.Id, p.Name, p.Description, docs.Count, p.CreatedAt, docs, categories, reqs, assignments);
    }
}
