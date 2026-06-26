using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Admin;

namespace TenderDocs.Application.Features.Projects;

public record ListProjectsQuery : IRequest<IReadOnlyList<ProjectDto>>;

public class ListProjectsHandler : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListProjectsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<ProjectDto>> Handle(ListProjectsQuery q, CancellationToken ct)
    {
        // Non-admins see only the projects they're assigned to (Administration → Project Access).
        var isAdmin = ProjectAccessScope.IsAdmin(_current);
        return await _db.Projects
            .Where(p => p.OrganizationId == _current.OrganizationId && !p.IsDeleted
                && (isAdmin || _db.UserProjects.Any(up => up.ProjectId == p.Id && up.UserId == _current.UserId)))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description,
                p.Assignments.Count, p.CreatedAt, p.CreatedById))
            .ToListAsync(ct);
    }
}
