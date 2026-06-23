using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

public class Project : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public string Name { get; set; } = default!;           // "IPHONE SUPPLY", "NHAI Bridge 2025"
    public string? Description { get; set; }                // "Demo tender bundle" / null -> "No description"

    public Guid? RootFolderId { get; set; }                 // optional dedicated folder tree for the project
    public Folder? RootFolder { get; set; }

    public ICollection<ProjectRequirementCategory> RequirementCategories { get; set; } = new List<ProjectRequirementCategory>();
    public ICollection<ProjectRequirement> Requirements { get; set; } = new List<ProjectRequirement>();
    public ICollection<ProjectDocumentAssignment> Assignments { get; set; } = new List<ProjectDocumentAssignment>();
}
