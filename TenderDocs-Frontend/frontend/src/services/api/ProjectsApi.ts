import { api } from '@/config/api';
import type { ProjectDto, ProjectDetailDto, ProjectSummaryDto } from './dtos';

export const ProjectsApi = {
  list: () => api.get<ProjectDto[]>('/projects'),
  summary: () => api.get<ProjectSummaryDto[]>('/projects/summary'),
  get: (id: string) => api.get<ProjectDetailDto>(`/projects/${id}`),

  create: (name: string, description?: string, requirements?: string[]) =>
    api.post<ProjectDto>('/projects', { name, description, requirements }),

  update: (id: string, input: { name?: string; description?: string }) =>
    api.put<ProjectDto>(`/projects/${id}`, input),

  remove: (id: string) => api.del<void>(`/projects/${id}`),

  addDocument: (projectId: string, documentId: string, requirementId?: string) =>
    api.post<void>(`/projects/${projectId}/documents`, { documentId, requirementId }),

  removeDocument: (projectId: string, documentId: string) =>
    api.del<void>(`/projects/${projectId}/documents/${documentId}`),

  setDocuments: (projectId: string, documentIds: string[]) =>
    api.put<ProjectDetailDto>(`/projects/${projectId}/documents`, { documentIds }),

  downloadZip: (projectId: string) => api.blob(`/projects/${projectId}/zip`),
};
