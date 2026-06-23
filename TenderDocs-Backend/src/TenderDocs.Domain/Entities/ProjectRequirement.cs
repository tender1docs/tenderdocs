using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

/// <summary>A required slot in a tender bundle, e.g. "GST", "PAN", "IT Returns" (see Image 6).</summary>
public class ProjectRequirement : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    /// <summary>The category (top-level group) this row belongs to. Null = ungrouped (legacy/orphan).</summary>
    public Guid? CategoryId { get; set; }
    public ProjectRequirementCategory? Category { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<ProjectDocumentAssignment> Assignments { get; set; } = new List<ProjectDocumentAssignment>();
}
