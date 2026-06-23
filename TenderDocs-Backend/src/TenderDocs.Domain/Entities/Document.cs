using TenderDocs.Domain.Common;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Entities;

public class Document : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public Guid? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public string Name { get; set; } = default!;           // "sample.pdf"
    public DocumentType DocumentType { get; set; }         // GST Certificate, etc.
    public string? IssuingAuthority { get; set; }          // "GST Department"
    public string? FinancialYear { get; set; }             // "FY 2024-25"
    public string? Notes { get; set; }

    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }              // drives Valid/Expiring/Expired

    // Storage descriptor (provider-agnostic)
    public StorageProviderType StorageProvider { get; set; } = StorageProviderType.Local;
    public string StorageKey { get; set; } = default!;     // local path / drive fileId / s3 key
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public string? Checksum { get; set; }

    public Guid? UploadedById { get; set; }
    public User? UploadedBy { get; set; }

    // Approval workflow — set by an Approver. New documents start Pending.
    public DocumentApprovalStatus ApprovalStatus { get; set; } = DocumentApprovalStatus.Pending;
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovalAt { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
    public ICollection<ProjectDocumentAssignment> Assignments { get; set; } = new List<ProjectDocumentAssignment>();

    /// <summary>Computes the label shown on cards. Threshold = days before expiry to flag "Expiring".</summary>
    public DocumentStatus ComputeStatus(int expiringThresholdDays = 30, DateOnly? today = null)
    {
        if (ExpiryDate is null) return DocumentStatus.NoExpiry;
        var now = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (ExpiryDate.Value < now) return DocumentStatus.Expired;
        if (ExpiryDate.Value <= now.AddDays(expiringThresholdDays)) return DocumentStatus.ExpiringSoon;
        return DocumentStatus.Valid;
    }
}
