using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

/// <summary>
/// Assigns a user to a project. Scopes which projects a user may access; an empty assignment set for a
/// non-Admin means "no project access yet". (Query-level enforcement across read paths lands in a later phase.)
/// </summary>
public class UserProject : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;
}
