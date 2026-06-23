using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

public class Tag : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;     // "urgent", "fy2025"
    public string? Color { get; set; }

    public ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
}

public class DocumentTag
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = default!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}
