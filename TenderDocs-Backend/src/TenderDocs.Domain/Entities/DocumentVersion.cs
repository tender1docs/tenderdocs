using TenderDocs.Domain.Common;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Entities;

public class DocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = default!;

    public int VersionNumber { get; set; }
    public StorageProviderType StorageProvider { get; set; }
    public string StorageKey { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public string? Checksum { get; set; }

    public Guid? UploadedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
