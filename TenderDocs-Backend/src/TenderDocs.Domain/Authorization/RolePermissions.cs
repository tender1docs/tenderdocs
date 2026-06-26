using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Authorization;

/// <summary>
/// The default role → permission mapping — the source of truth that seeds the
/// <c>role_permissions</c> table and backs every authorization check. Editing a role's
/// capabilities (or introducing a new role) means changing this map / table, never the
/// endpoint guards.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<UserRole, IReadOnlySet<string>> Map =
        new Dictionary<UserRole, IReadOnlySet<string>>
        {
            // Admin — the only unrestricted role: every permission.
            [UserRole.Admin] = Permissions.AllKeys,

            // Uploader — document work only.
            [UserRole.Uploader] = new HashSet<string>
            {
                Permissions.Documents.Read,
                Permissions.Documents.Upload,
                Permissions.Documents.Edit,
                Permissions.Documents.Download,
                Permissions.Projects.Read,
                Permissions.Projects.Assign,
                Permissions.Organize.Read,
                Permissions.Organize.Edit,
                Permissions.Notifications.Read,
            },

            // Approver — approval only: review, approve, reject. No upload / edit / delete.
            [UserRole.Approver] = new HashSet<string>
            {
                Permissions.Documents.Read,
                Permissions.Documents.Download,
                Permissions.Documents.Approve,
                Permissions.Projects.Read,
                Permissions.Organize.Read,
                Permissions.Notifications.Read,
            },

            // Viewer — read-only.
            [UserRole.Viewer] = new HashSet<string>
            {
                Permissions.Documents.Read,
                Permissions.Documents.Download,
                Permissions.Projects.Read,
                Permissions.Organize.Read,
                Permissions.Notifications.Read,
            },
        };

    /// <summary>The permission set granted to a role (empty for an unknown role).</summary>
    public static IReadOnlySet<string> For(UserRole role) =>
        Map.TryGetValue(role, out var perms) ? perms : new HashSet<string>();

    /// <summary>Whether a role grants a given permission key.</summary>
    public static bool Has(UserRole role, string permission) => For(role).Contains(permission);
}
