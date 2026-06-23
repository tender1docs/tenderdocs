using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

/// <summary>
/// A top-level group in a project's Organize workspace, e.g. "Financial" or "Technical".
/// Each category owns a set of <see cref="ProjectRequirement"/> rows (its sub-categories) and maps
/// to a folder in the exported ZIP. Categories are per-project and fully renamable.
/// </summary>
public class ProjectRequirementCategory : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int SortOrder { get; set; }

    public ICollection<ProjectRequirement> Requirements { get; set; } = new List<ProjectRequirement>();
}
