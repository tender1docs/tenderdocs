using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;

namespace TenderDocs.Application.Features.Documents;

public record UploadDocumentCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long FileSize,
    DocumentType DocumentType,
    string? IssuingAuthority,
    string? FinancialYear,
    string? Notes,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    Guid? FolderId,
    Guid? ProjectId,
    IReadOnlyList<string>? Tags) : IRequest<DocumentDto>;

public class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileSize).GreaterThan(0);
    }
}

public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IStorageProviderFactory _storageFactory;
    private readonly IDateTime _clock;
    private readonly IDocumentCompressor _compressor;
    private readonly IAuditLogger _audit;

    public UploadDocumentHandler(IAppDbContext db, ICurrentUser current,
        IStorageProviderFactory storageFactory, IDateTime clock, IDocumentCompressor compressor, IAuditLogger audit)
        => (_db, _current, _storageFactory, _clock, _compressor, _audit) = (db, current, storageFactory, clock, compressor, audit);

    public async Task<DocumentDto> Handle(UploadDocumentCommand r, CancellationToken ct)
    {
        var orgId = _current.OrganizationId!.Value;
        var provider = await _storageFactory.GetActiveProviderAsync(orgId, ct);

        string? folderKey = null;
        if (r.FolderId is not null)
            folderKey = (await _db.Folders.FirstOrDefaultAsync(f => f.Id == r.FolderId, ct))?.MaterializedPath;

        // Shrink the file (re-encode images, recompress PDFs/Office media) before storing.
        // Falls back to the original bytes for unsupported types or if compression doesn't help.
        await using var compressed = await _compressor.CompressAsync(r.Content, r.FileName, r.ContentType, ct);

        var stored = await provider.UploadFileAsync(
            compressed.Content, compressed.FileName, compressed.ContentType, folderKey, ct);

        var doc = new Document
        {
            OrganizationId = orgId,
            FolderId = r.FolderId,
            Name = compressed.FileName,
            DocumentType = r.DocumentType,
            IssuingAuthority = r.IssuingAuthority,
            FinancialYear = r.FinancialYear,
            Notes = r.Notes,
            IssueDate = r.IssueDate,
            ExpiryDate = r.ExpiryDate,
            StorageProvider = provider.ProviderType,
            StorageKey = stored.Key,
            FileSizeBytes = stored.SizeBytes == 0 ? compressed.CompressedSizeBytes : stored.SizeBytes,
            ContentType = compressed.ContentType,
            Checksum = stored.Checksum,
            UploadedById = _current.UserId,
            CreatedAt = _clock.UtcNow
        };
        _db.Documents.Add(doc);

        await AttachTagsAsync(doc, orgId, r.Tags, ct);

        if (r.ProjectId is not null)
            _db.ProjectDocumentAssignments.Add(new ProjectDocumentAssignment
            {
                ProjectId = r.ProjectId.Value, DocumentId = doc.Id,
                AssignedById = _current.UserId, CreatedAt = _clock.UtcNow
            });

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Upload, "Document", doc.Id,
            new { doc.Name, type = doc.DocumentType.ToString() }, ct: ct);

        var saved = await _db.Documents.Include(d => d.UploadedBy)
            .Include(d => d.DocumentTags).ThenInclude(t => t.Tag)
            .FirstAsync(d => d.Id == doc.Id, ct);
        return DocumentMapping.ToDto(saved);
    }

    private async Task AttachTagsAsync(Document doc, Guid orgId, IReadOnlyList<string>? tags, CancellationToken ct)
    {
        if (tags is null) return;
        foreach (var name in tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct())
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.OrganizationId == orgId && t.Name == name, ct);
            if (tag is null)
            {
                tag = new Tag { OrganizationId = orgId, Name = name, CreatedAt = _clock.UtcNow };
                _db.Tags.Add(tag);
            }
            doc.DocumentTags.Add(new DocumentTag { Document = doc, Tag = tag });
        }
    }
}
