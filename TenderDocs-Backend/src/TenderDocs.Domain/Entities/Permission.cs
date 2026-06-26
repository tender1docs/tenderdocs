using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

/// <summary>
/// A fine-grained permission in the catalog (seeded from Domain.Authorization.Permissions).
/// Reference data that powers the Roles permission-matrix view; authorization itself is resolved
/// from the role → permission map.
/// </summary>
public class Permission : BaseEntity
{
    public string Key { get; set; } = default!;          // e.g. "documents.upload"
    public string Category { get; set; } = default!;     // e.g. "Documents"
    public string Description { get; set; } = default!;
}
