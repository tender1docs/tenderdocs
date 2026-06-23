using TenderDocs.Domain.Common;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Entities;

public class User : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string? PasswordHash { get; set; }          // null for pure Google-OAuth users
    public string FullName { get; set; } = default!;
    public string Initials { get; set; } = default!;   // "PG" avatar in the top-right
    public UserRole Role { get; set; } = UserRole.Viewer;

    public string? GoogleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
