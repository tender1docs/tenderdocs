import { api } from '@/config/api';

export interface AuditLogDto {
  id: string; action: string; entityType: string; entityId: string | null;
  userId: string | null; userName: string | null; detailsJson: string | null;
  ipAddress: string | null; createdAt: string;
}
export interface PagedAuditLogs { items: AuditLogDto[]; page: number; pageSize: number; totalCount: number }

export interface StorageStatsDto {
  provider: string; googleDriveConnected: boolean; folderId: string | null;
  usedBytes: number; documentCount: number; projectCount: number; healthy: boolean;
}

export interface PermissionDefDto { key: string; category: string; description: string }
export interface RoleGrantsDto { role: string; permissions: string[] }
export interface RolesMatrixDto { permissions: PermissionDefDto[]; roles: RoleGrantsDto[] }

export interface ApprovalQueueItemDto {
  documentId: string; name: string; documentType: string; uploadedByName: string | null;
  uploadedAt: string; projects: string; approvalStatus: string;
}

export interface AdminNotificationDto {
  id: string; title: string; message: string; userId: string;
  userName: string | null; isRead: boolean; createdAt: string;
}

export const AdminApi = {
  audit: (params: { userId?: string; action?: string; from?: string; to?: string; page?: number; pageSize?: number } = {}) => {
    const qs = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => { if (v !== undefined && v !== null && `${v}` !== '') qs.set(k, `${v}`); });
    const s = qs.toString();
    return api.get<PagedAuditLogs>(`/admin/audit${s ? `?${s}` : ''}`);
  },
  storage: () => api.get<StorageStatsDto>('/admin/storage'),
  roles: () => api.get<RolesMatrixDto>('/admin/roles'),
  approvals: () => api.get<ApprovalQueueItemDto[]>('/admin/approvals'),
  userProjects: (userId: string) => api.get<string[]>(`/admin/users/${userId}/projects`),
  setUserProjects: (userId: string, projectIds: string[]) =>
    api.put<string[]>(`/admin/users/${userId}/projects`, { projectIds }),
  notifications: () => api.get<AdminNotificationDto[]>('/admin/notifications'),
  broadcast: (input: { target: string; userId?: string; title: string; message: string }) =>
    api.post<{ recipients: number }>('/admin/notifications/broadcast', input),
  report: (type: string) => api.blob(`/admin/reports/${type}`),
};
