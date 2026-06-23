using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Projects;

/// <summary>
/// Lightweight project list that also returns the IDs of the documents assigned to each
/// project. The frontend needs the IDs (not just a count) to show per-project document
/// counts and to filter the documents page "by project".
/// </summary>
public record ProjectSummaryDto(
    Guid Id, string Name, string? Description, DateTimeOffset CreatedAt,
    Guid? CreatedById, IReadOnlyList<Guid> DocumentIds);

public record ListProjectSummariesQuery : IRequest<IReadOnlyList<ProjectSummaryDto>>;

public class ListProjectSummariesHandler
    : IRequestHandler<ListProjectSummariesQuery, IReadOnlyList<ProjectSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListProjectSummariesHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<ProjectSummaryDto>> Handle(ListProjectSummariesQuery q, CancellationToken ct)
        => await _db.Projects
            .Where(p => p.OrganizationId == _current.OrganizationId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectSummaryDto(
                p.Id, p.Name, p.Description, p.CreatedAt, p.CreatedById,
                p.Assignments
                    .Where(a => !a.Document.IsDeleted)
                    .Select(a => a.DocumentId)
                    .ToList()))
            .ToListAsync(ct);
}
