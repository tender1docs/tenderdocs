using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Documents;

public record UpdateDocumentCommand(
    Guid Id, string? Name, DocumentType? DocumentType, string? IssuingAuthority,
    string? FinancialYear, string? Notes, DateOnly? IssueDate, DateOnly? ExpiryDate,
    Guid? FolderId, IReadOnlyList<string>? Tags) : IRequest<DocumentDto>;

public class UpdateDocumentHandler : IRequestHandler<UpdateDocumentCommand, DocumentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public UpdateDocumentHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task<DocumentDto> Handle(UpdateDocumentCommand r, CancellationToken ct)
    {
        var d = await _db.Documents.Include(x => x.DocumentTags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(x => x.Id == r.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Document", r.Id);

        if (r.Name is not null) d.Name = r.Name;
        if (r.DocumentType is not null) d.DocumentType = r.DocumentType.Value;
        if (r.IssuingAuthority is not null) d.IssuingAuthority = r.IssuingAuthority;
        if (r.FinancialYear is not null) d.FinancialYear = r.FinancialYear;
        if (r.Notes is not null) d.Notes = r.Notes;
        if (r.IssueDate is not null) d.IssueDate = r.IssueDate;
        if (r.ExpiryDate is not null) d.ExpiryDate = r.ExpiryDate;
        if (r.FolderId is not null) d.FolderId = r.FolderId;
        d.UpdatedAt = _clock.UtcNow;

        if (r.Tags is not null)
        {
            d.DocumentTags.Clear();
            foreach (var name in r.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct())
            {
                var tag = await _db.Tags.FirstOrDefaultAsync(t => t.OrganizationId == d.OrganizationId && t.Name == name, ct)
                          ?? new Tag { OrganizationId = d.OrganizationId, Name = name, CreatedAt = _clock.UtcNow };
                if (tag.Id == Guid.Empty || !await _db.Tags.AnyAsync(t => t.Id == tag.Id, ct)) _db.Tags.Add(tag);
                d.DocumentTags.Add(new DocumentTag { DocumentId = d.Id, Tag = tag });
            }
        }

        await _db.SaveChangesAsync(ct);
        var saved = await _db.Documents.Include(x => x.UploadedBy)
            .Include(x => x.DocumentTags).ThenInclude(t => t.Tag).FirstAsync(x => x.Id == d.Id, ct);
        return DocumentMapping.ToDto(saved);
    }
}
