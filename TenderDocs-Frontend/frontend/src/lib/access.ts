/**
 * Frontend permission gating. The permission keys mirror the backend catalog
 * (TenderDocs.Domain/Authorization/Permissions.cs). The authoritative permission set for the
 * signed-in user is delivered by /auth/me and held in AuthProvider (`permissions`); these helpers
 * only decide what UI to show. The API independently enforces every permission, so hiding UI is a
 * convenience, never the security boundary.
 */

/** Permission keys — keep in sync with the backend Permissions catalog. */
export const Permission = {
  DocumentsRead: 'documents.read',
  DocumentsUpload: 'documents.upload',
  DocumentsEdit: 'documents.edit',
  DocumentsDelete: 'documents.delete',
  DocumentsApprove: 'documents.approve',
  DocumentsDownload: 'documents.download',
  ProjectsRead: 'projects.read',
  ProjectsManage: 'projects.manage',
  ProjectsAssign: 'projects.assign',
  OrganizeRead: 'organize.read',
  OrganizeEdit: 'organize.edit',
  StorageRead: 'storage.read',
  StorageManage: 'storage.manage',
  UsersRead: 'users.read',
  UsersManage: 'users.manage',
  RolesManage: 'roles.manage',
  ProjectAccessManage: 'projectAccess.manage',
  AuditView: 'audit.view',
  NotificationsRead: 'notifications.read',
  NotificationsManage: 'notifications.manage',
  ReportsView: 'reports.view',
  AdminAccess: 'admin.access',
} as const;

export type PermissionKey = (typeof Permission)[keyof typeof Permission];

type Perms = readonly string[] | null | undefined;

/** Whether the signed-in user holds a permission. */
export function can(permissions: Perms, permission: string): boolean {
  return !!permissions && permissions.includes(permission);
}

/** Whether the user holds any of the listed permissions. */
export function canAny(permissions: Perms, ...keys: string[]): boolean {
  return !!permissions && keys.some((k) => permissions.includes(k));
}

/**
 * Whether the user may visit a path. Only the Administration area is gated here — every other
 * authenticated page (dashboard, documents, projects, organize, settings) is open to all roles, and
 * the destructive actions inside them are gated individually with {@link can}.
 */
export function canVisitPath(permissions: Perms, pathname: string): boolean {
  if (pathname.startsWith('/admin')) return can(permissions, Permission.AdminAccess);
  return true;
}
