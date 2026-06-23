using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Documents;

namespace TenderDocs.Application.Features.Search;

public record GlobalSearchResultDto(
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<SearchProjectHit> Projects);

public record SearchProjectHit(Guid Id, string Name, string? Description);

public record GlobalSearchQuery(string Term) : IRequest<GlobalSearchResultDto>;

public class GlobalSearchHandler : IRequestHandler<GlobalSearchQuery, GlobalSearchResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GlobalSearchHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<GlobalSearchResultDto> Handle(GlobalSearchQuery q, CancellationToken ct)
    {
        var s = (q.Term ?? "").Trim().ToLower();
        var orgId = _current.OrganizationId;
        if (s.Length == 0) return new GlobalSearchResultDto(Array.Empty<DocumentDto>(), Array.Empty<SearchProjectHit>());

        var docs = await _db.Documents
            .Include(d => d.UploadedBy).Include(d => d.DocumentTags).ThenInclude(t => t.Tag)
            .Where(d => d.OrganizationId == orgId && !d.IsDeleted &&
                (d.Name.ToLower().Contains(s) ||
                 (d.Notes != null && d.Notes.ToLower().Contains(s)) ||
                 (d.IssuingAuthority != null && d.IssuingAuthority.ToLower().Contains(s)) ||
                 (d.FinancialYear != null && d.FinancialYear.ToLower().Contains(s)) ||
                 d.DocumentTags.Any(t => t.Tag.Name.ToLower().Contains(s))))
            .OrderByDescending(d => d.CreatedAt).Take(25).ToListAsync(ct);

        var projects = await _db.Projects
            .Where(p => p.OrganizationId == orgId && !p.IsDeleted &&
                (p.Name.ToLower().Contains(s) || (p.Description != null && p.Description.ToLower().Contains(s))))
            .Take(10)
            .Select(p => new SearchProjectHit(p.Id, p.Name, p.Description)).ToListAsync(ct);

        return new GlobalSearchResultDto(docs.Select(d => DocumentMapping.ToDto(d)).ToList(), projects);
    }
}
