import { api } from '@/config/api';
import type { StorageStatusDto, FolderNodeDto, TeamMemberDto } from './dtos';

export const StorageApi = {
  status: () => api.get<StorageStatusDto>('/storage/status'),
  tree: (projectId?: string) =>
    api.get<FolderNodeDto[]>(`/storage/tree${projectId ? `?projectId=${projectId}` : ''}`),

  /** Begin the Google Drive OAuth flow — returns the Google consent URL to redirect to. */
  authorizeGoogleDrive: () => api.get<{ url: string }>('/google-drive/authorize'),

  connectGoogleDrive: (input: {
    clientId: string; clientSecret: string; redirectUri: string; folderId: string;
  }) => api.post<StorageStatusDto>('/google-drive/connect', input),
  disconnectGoogleDrive: () => api.post<void>('/google-drive/disconnect'),
};

export const UsersApi = {
  list: () => api.get<TeamMemberDto[]>('/users'),
  create: (input: { email: string; fullName: string; password: string; role: 'Admin' | 'Manager' | 'Viewer' }) =>
    api.post<TeamMemberDto>('/users', input),
};
