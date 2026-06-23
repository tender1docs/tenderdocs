import { api } from '@/config/api';
import type { ProjectDetailDto, FolderNodeDto } from './dtos';

/** Organize module — mirrors /api/organize on the backend. */
export const OrganizeApi = {
  project: (id: string) => api.get<ProjectDetailDto>(`/organize/project/${id}`),

  ensureRequirements: (projectId: string) =>
    api.post<ProjectDetailDto>(`/organize/ensure-requirements/${projectId}`),

  assign: (projectId: string, documentId: string, requirementId?: string) =>
    api.post<void>('/organize/assign-document', { projectId, documentId, requirementId }),

  unassign: (projectId: string, documentId: string) =>
    api.del<void>('/organize/unassign-document', { projectId, documentId }),

  tree: (projectId: string) => api.get<FolderNodeDto[]>(`/organize/tree/${projectId}`),

  export: (projectId: string) => api.blob(`/organize/export/${projectId}`),

  // ---- Editable structure (categories + sub-category rows). All return the refreshed project. ----
  createCategory: (projectId: string, name: string) =>
    api.post<ProjectDetailDto>('/organize/category', { projectId, name }),
  renameCategory: (projectId: string, categoryId: string, name: string) =>
    api.put<ProjectDetailDto>(`/organize/category/${projectId}/${categoryId}`, { name }),
  deleteCategory: (projectId: string, categoryId: string) =>
    api.del<ProjectDetailDto>(`/organize/category/${projectId}/${categoryId}`),

  createRequirement: (projectId: string, categoryId: string, name: string) =>
    api.post<ProjectDetailDto>('/organize/requirement', { projectId, categoryId, name }),
  renameRequirement: (projectId: string, requirementId: string, name: string) =>
    api.put<ProjectDetailDto>(`/organize/requirement/${projectId}/${requirementId}`, { name }),
  deleteRequirement: (projectId: string, requirementId: string) =>
    api.del<ProjectDetailDto>(`/organize/requirement/${projectId}/${requirementId}`),
};
