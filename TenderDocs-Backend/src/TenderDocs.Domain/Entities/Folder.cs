using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

/// <summary>
/// Unlimited-depth folder tree. Uses BOTH adjacency list (ParentFolderId) and
/// materialized path (MaterializedPath) so we can retrieve a whole subtree in one query.
/// Path format: "/{rootId}/{childId}/{grandchildId}/" using lowercase Guids without dashes.
/// </summary>
public class Folder : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> Children { get; set; } = new List<Folder>();

    public string Name { get; set; } = default!;
    public string MaterializedPath { get; set; } = "/"; // e.g. "/a1b2.../c3d4.../"
    public int Depth { get; set; }                       // 0 = root

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
