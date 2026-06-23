using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Documents;
using TenderDocs.Application.Features.Projects;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Dashboard;

// Powers the dashboard cards, "Documents by Type" bar chart, "Type Distribution" donut,
// "Expiring / Expired" list, Projects panel, and "Recent Uploads".
public record DashboardStatsDto(
    int TotalDocuments, int Valid, int ExpiringSoon, int Expired,
    IReadOnlyList<TypeCountDto> DocumentsByType,
    IReadOnlyList<DocumentDto> ExpiringOrExpired,
    IReadOnlyList<ProjectDto> RecentProjects,
    IReadOnlyList<DocumentDto> RecentUploads);

public record TypeCountDto(string Type, string Label, int Count);

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GetDashboardStatsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery q, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var docs = await _db.Documents
            .Include(d => d.UploadedBy).Include(d => d.DocumentTags).ThenInclude(t => t.Tag)
            .Where(d => d.OrganizationId == orgId && !d.IsDeleted)
            .ToListAsync(ct);

        var total = docs.Count;
        var valid = docs.Count(d => d.ComputeStatus() == DocumentStatus.Valid || d.ComputeStatus() == DocumentStatus.NoExpiry);
        var expiring = docs.Count(d => d.ComputeStatus() == DocumentStatus.ExpiringSoon);
        var expired = docs.Count(d => d.ComputeStatus() == DocumentStatus.Expired);

        var byType = docs.GroupBy(d => d.DocumentType)
            .Select(g => new TypeCountDto(g.Key.ToString(), DocumentMapping.Label(g.Key), g.Count()))
            .OrderByDescending(t => t.Count).ToList();

        var expiringList = docs
            .Where(d => d.ComputeStatus() is DocumentStatus.ExpiringSoon or DocumentStatus.Expired)
            .OrderBy(d => d.ExpiryDate)
            .Select(d => DocumentMapping.ToDto(d)).ToList();

        var recentUploads = docs.OrderByDescending(d => d.CreatedAt).Take(5)
            .Select(d => DocumentMapping.ToDto(d)).ToList();

        var recentProjects = await _db.Projects
            .Where(p => p.OrganizationId == orgId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt).Take(5)
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.Assignments.Count, p.CreatedAt, p.CreatedById))
            .ToListAsync(ct);

        return new DashboardStatsDto(total, valid, expiring, expired,
            byType, expiringList, recentProjects, recentUploads);
    }
}
