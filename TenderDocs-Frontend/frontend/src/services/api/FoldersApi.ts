import { api } from '@/config/api';
import type { FolderNodeDto } from './dtos';

export const FoldersApi = {
  tree: (projectId?: string) =>
    api.get<FolderNodeDto[]>(`/folders/tree${projectId ? `?projectId=${projectId}` : ''}`),

  create: (name: string, parentFolderId?: string, projectId?: string) =>
    api.post<FolderNodeDto>('/folders', { name, parentFolderId, projectId }),

  move: (id: string, newParentFolderId?: string) =>
    api.post<void>(`/folders/${id}/move`, { newParentFolderId }),

  remove: (id: string) => api.del<void>(`/folders/${id}`),
};
