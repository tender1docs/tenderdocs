namespace TenderDocs.Application.Features.Documents;

public record DocumentDto(
    Guid Id,
    string Name,
    string DocumentType,        // enum name
    string DocumentTypeLabel,   // "GST Certificate"
    string? IssuingAuthority,
    string? FinancialYear,
    string? Notes,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string Status,              // Valid / ExpiringSoon / Expired / NoExpiry
    string StorageProvider,
    long FileSizeBytes,
    string ContentType,
    Guid? FolderId,
    Guid? UploadedById,
    string? UploadedByName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Tags,
    string ApprovalStatus,        // Pending / Approved / Rejected
    string? ApprovedByName,
    DateTimeOffset? ApprovalAt,
    string? RejectionReason);

public record DocumentTypeOption(string Value, string Label);
