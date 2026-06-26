namespace TenderDocs.Domain.Authorization;

/// <summary>
/// The canonical catalog of fine-grained permissions. Authorization is permission-based:
/// roles map to sets of these keys (see <see cref="RolePermissions"/>), and API endpoints are
/// guarded by a permission key rather than a role name. Adding a future role (e.g. "Project
/// Manager") is therefore a matter of assigning permissions, not rewriting authorization logic.
///
/// Keys use a stable <c>area.action</c> convention and are shared verbatim with the frontend
/// permission layer, so the two never drift.
/// </summary>
public static class Permissions
{
    public static class Documents
    {
        public const string Read = "documents.read";
        public const string Upload = "documents.upload";
        public const string Edit = "documents.edit";
        public const string Delete = "documents.delete";
        public const string Approve = "documents.approve";   // approve / reject / request changes
        public const string Download = "documents.download";
    }

    public static class Projects
    {
        public const string Read = "projects.read";
        public const string Manage = "projects.manage";       // create / edit / delete a project
        public const string Assign = "projects.assign";       // attach / detach documents
    }

    public static class Organize
    {
        public const string Read = "organize.read";
        public const string Edit = "organize.edit";           // categories, requirements, folder tree
    }

    public static class Storage
    {
        public const string Read = "storage.read";
        public const string Manage = "storage.manage";        // connect / disconnect / configure providers
    }

    public static class Users
    {
        public const string Read = "users.read";
        public const string Manage = "users.manage";          // create / edit / disable / delete / reset / assign
    }

    public static class Roles
    {
        public const string Manage = "roles.manage";          // edit the role → permission matrix
    }

    public static class ProjectAccess
    {
        public const string Manage = "projectAccess.manage";  // assign user ↔ project
    }

    public static class Audit
    {
        public const string View = "audit.view";
    }

    public static class Notifications
    {
        public const string Read = "notifications.read";
        public const string Manage = "notifications.manage";  // broadcast
    }

    public static class Reports
    {
        public const string View = "reports.view";
    }

    public static class Admin
    {
        public const string Access = "admin.access";          // gates the whole Administration portal
    }

    /// <summary>One definition per permission, used to seed the catalog and render the Roles matrix.</summary>
    public record Definition(string Key, string Category, string Description);

    /// <summary>Every permission in the system, in display order.</summary>
    public static readonly IReadOnlyList<Definition> All = new[]
    {
        new Definition(Documents.Read,     "Documents", "View documents"),
        new Definition(Documents.Upload,   "Documents", "Upload documents"),
        new Definition(Documents.Edit,     "Documents", "Edit document metadata / replace files"),
        new Definition(Documents.Delete,   "Documents", "Delete documents"),
        new Definition(Documents.Approve,  "Documents", "Approve / reject / request changes"),
        new Definition(Documents.Download, "Documents", "Download documents"),

        new Definition(Projects.Read,      "Projects",  "View projects"),
        new Definition(Projects.Manage,    "Projects",  "Create / edit / delete projects"),
        new Definition(Projects.Assign,    "Projects",  "Attach / detach documents to projects"),

        new Definition(Organize.Read,      "Organize",  "View the organize workspace"),
        new Definition(Organize.Edit,      "Organize",  "Edit categories, requirements, folders"),

        new Definition(Storage.Read,       "Storage",   "View storage status"),
        new Definition(Storage.Manage,     "Storage",   "Connect / configure storage providers"),

        new Definition(Users.Read,         "Users",     "View users"),
        new Definition(Users.Manage,       "Users",     "Create / edit / disable / delete users"),

        new Definition(Roles.Manage,       "Roles",     "Manage the role → permission matrix"),
        new Definition(ProjectAccess.Manage, "Access",  "Assign users to projects"),

        new Definition(Audit.View,         "Audit",     "View audit logs"),

        new Definition(Notifications.Read,   "Notifications", "View notifications"),
        new Definition(Notifications.Manage, "Notifications", "Broadcast notifications"),

        new Definition(Reports.View,       "Reports",   "Generate reports"),

        new Definition(Admin.Access,       "Admin",     "Access the administration portal"),
    };

    /// <summary>All permission keys (used for the Admin wildcard).</summary>
    public static readonly IReadOnlySet<string> AllKeys =
        All.Select(d => d.Key).ToHashSet();
}
