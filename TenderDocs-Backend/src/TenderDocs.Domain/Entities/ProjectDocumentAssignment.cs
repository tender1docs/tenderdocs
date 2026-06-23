using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

/// <summary>Links a document into a project (optionally fulfilling a specific requirement slot).</summary>
public class ProjectDocumentAssignment : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = default!;

    public Guid? RequirementId { get; set; }
    public ProjectRequirement? Requirement { get; set; }

    public Guid? AssignedById { get; set; }
}
