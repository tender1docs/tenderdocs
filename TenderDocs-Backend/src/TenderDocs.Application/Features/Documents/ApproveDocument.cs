using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;

namespace TenderDocs.Application.Features.Documents;

// Approver-only review actions on a document. Approval lives on the document itself, so the
// decision is reflected everywhere the document appears (Documents list, project detail, Organize).
public record ApproveDocumentCommand(Guid Id) : IRequest<DocumentDto>;
public record RejectDocumentCommand(Guid Id, string? Reason) : IRequest<DocumentDto>;

public class ApproveDocumentHandler : IRequestHandler<ApproveDocumentCommand, DocumentDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current; private readonly IDateTime _clock; private readonly IAuditLogger _audit;
    private readonly IStorageProviderFactory _storageFactory; private readonly IGoogleOAuthService _google;
    public ApproveDocumentHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IAuditLogger audit,
        IStorageProviderFactory storageFactory, IGoogleOAuthService google)
        => (_db, _current, _clock, _audit, _storageFactory, _google) = (db, current, clock, audit, storageFactory, google);

    // On approval, promote the file from the staging folder to the master (client-facing) Drive folder.
    public Task<DocumentDto> Handle(ApproveDocumentCommand r, CancellationToken ct)
        => DocumentReview.ApplyAsync(_db, _current, _clock, _audit, r.Id, DocumentApprovalStatus.Approved, null, ct,
            _storageFactory, _google.GetDriveConfig().MasterFolderId);
}

public class RejectDocumentHandler : IRequestHandler<RejectDocumentCommand, DocumentDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current; private readonly IDateTime _clock; private readonly IAuditLogger _audit;
    public RejectDocumentHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IAuditLogger audit)
        => (_db, _current, _clock, _audit) = (db, current, clock, audit);

    public Task<DocumentDto> Handle(RejectDocumentCommand r, CancellationToken ct)
        => DocumentReview.ApplyAsync(_db, _current, _clock, _audit, r.Id, DocumentApprovalStatus.Rejected, r.Reason, ct);
}

internal static class DocumentReview
{
    public static async Task<DocumentDto> ApplyAsync(IAppDbContext db, ICurrentUser current, IDateTime clock,
        IAuditLogger audit, Guid id, DocumentApprovalStatus status, string? reason, CancellationToken ct,
        IStorageProviderFactory? storageFactory = null, string? masterFolderId = null)
    {
        var d = await db.Documents
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Document", id);

        // When approved, move the underlying file out of the staging folder and into the master
        // (client-facing) Google Drive folder. Drive-backed docs only, and only when a master folder
        // is configured. A move failure aborts the approval so the DB and Drive stay consistent.
        if (status == DocumentApprovalStatus.Approved
            && storageFactory is not null
            && !string.IsNullOrWhiteSpace(masterFolderId)
            && d.StorageProvider == StorageProviderType.GoogleDrive
            && !string.IsNullOrWhiteSpace(d.StorageKey))
        {
            var provider = await storageFactory.GetActiveProviderAsync(d.OrganizationId, ct);
            if (provider.ProviderType == StorageProviderType.GoogleDrive)
                d.StorageKey = await provider.MoveFileAsync(d.StorageKey, masterFolderId!, ct);
        }

        d.ApprovalStatus = status;
        d.ApprovedById = current.UserId;
        d.ApprovalAt = clock.UtcNow;
        d.RejectionReason = status == DocumentApprovalStatus.Rejected
            ? (string.IsNullOrWhiteSpace(reason) ? null : reason.Trim())
            : null;
        d.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(AuditAction.Update, "Document", d.Id,
            new { d.Name, approval = status.ToString(), reason }, ct: ct);

        var saved = await db.Documents.Include(x => x.UploadedBy).Include(x => x.ApprovedBy)
            .Include(x => x.DocumentTags).ThenInclude(t => t.Tag).FirstAsync(x => x.Id == d.Id, ct);
        return DocumentMapping.ToDto(saved);
    }
}
