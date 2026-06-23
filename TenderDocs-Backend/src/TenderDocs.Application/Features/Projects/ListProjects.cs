using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Projects;

public record ListProjectsQuery : IRequest<IReadOnlyList<ProjectDto>>;

public class ListProjectsHandler : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListProjectsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<ProjectDto>> Handle(ListProjectsQuery q, CancellationToken ct)
        => await _db.Projects
            .Where(p => p.OrganizationId == _current.OrganizationId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description,
                p.Assignments.Count, p.CreatedAt, p.CreatedById))
            .ToListAsync(ct);
}
