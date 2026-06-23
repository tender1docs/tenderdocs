using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Common.Models;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Documents;

/// <summary>Matches the Documents page filter panel + search box.</summary>
public record ListDocumentsQuery(
    string? Search,
    DocumentType? DocumentType,
    string? IssuingAuthority,
    string? FinancialYear,
    Guid? ProjectId,
    Guid? UploaderId,
    string? ExpiryFilter,   // all | valid | expiring | expired | noexpiry
    string? Tag,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedList<DocumentDto>>;

public class ListDocumentsHandler : IRequestHandler<ListDocumentsQuery, PagedList<DocumentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListDocumentsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<PagedList<DocumentDto>> Handle(ListDocumentsQuery q, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;
        var query = _db.Documents
            .Include(d => d.UploadedBy)
            .Include(d => d.ApprovedBy)
            .Include(d => d.DocumentTags).ThenInclude(t => t.Tag)
            .Where(d => d.OrganizationId == orgId && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(s) ||
                (d.Notes != null && d.Notes.ToLower().Contains(s)) ||
                (d.IssuingAuthority != null && d.IssuingAuthority.ToLower().Contains(s)) ||
                d.DocumentTags.Any(t => t.Tag.Name.ToLower().Contains(s)));
        }
        if (q.DocumentType is not null) query = query.Where(d => d.DocumentType == q.DocumentType);
        if (!string.IsNullOrWhiteSpace(q.IssuingAuthority)) query = query.Where(d => d.IssuingAuthority == q.IssuingAuthority);
        if (!string.IsNullOrWhiteSpace(q.FinancialYear)) query = query.Where(d => d.FinancialYear == q.FinancialYear);
        if (q.UploaderId is not null) query = query.Where(d => d.UploadedById == q.UploaderId);
        if (q.ProjectId is not null)
            query = query.Where(d => d.Assignments.Any(a => a.ProjectId == q.ProjectId));
        if (!string.IsNullOrWhiteSpace(q.Tag))
            query = query.Where(d => d.DocumentTags.Any(t => t.Tag.Name == q.Tag));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        query = q.ExpiryFilter?.ToLowerInvariant() switch
        {
            "valid" => query.Where(d => d.ExpiryDate != null && d.ExpiryDate > today.AddDays(30)),
            "expiring" => query.Where(d => d.ExpiryDate != null && d.ExpiryDate >= today && d.ExpiryDate <= today.AddDays(30)),
            "expired" => query.Where(d => d.ExpiryDate != null && d.ExpiryDate < today),
            "noexpiry" => query.Where(d => d.ExpiryDate == null),
            _ => query
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .ToListAsync(ct);

        return new PagedList<DocumentDto>
        {
            Items = items.Select(d => DocumentMapping.ToDto(d)).ToList(),
            Page = q.Page, PageSize = q.PageSize, TotalCount = total
        };
    }
}
