using TenderDocs.Domain.Common;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Entities;

/// <summary>
/// Grants a permission to a role. Seeded from Domain.Authorization.RolePermissions and intended to be
/// the future-editable source of the role → permission matrix (so new roles are configured, not coded).
/// </summary>
public class RolePermission : BaseEntity
{
    public UserRole Role { get; set; }
    public string PermissionKey { get; set; } = default!;
}
