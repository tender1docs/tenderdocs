import { api } from '@/config/api';
import type { DocumentDto, PagedList } from './dtos';

// Maps the frontend category name to the backend DocumentType enum integer.
const DOCUMENT_TYPE_INT: Record<string, number> = {
  Gst: 0, Pan: 1, Itr: 2, Msme: 3, BalanceSheet: 4, TenderForm: 5,
  Iso: 6, ExperienceCertificate: 7, BankStatement: 8, FinancialDocument: 9,
  TechnicalDocument: 10, Other: 99,
};

export interface DocumentFilters {
  search?: string;
  documentType?: string;
  issuingAuthority?: string;
  financialYear?: string;
  projectId?: string;
  uploaderId?: string;
  expiryFilter?: string;
  tag?: string;
  page?: number;
  pageSize?: number;
}

export interface UploadDocumentInput {
  file: File;
  documentType?: string;        // enum name; defaults to "Other"
  issuingAuthority?: string;
  financialYear?: string;
  notes?: string;
  issueDate?: string;
  expiryDate?: string;
  folderId?: string;
  projectId?: string;
  tags?: string;                // comma-separated
}

function qs(filters: DocumentFilters): string {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([k, v]) => {
    if (v !== undefined && v !== null && `${v}` !== '') params.set(k, `${v}`);
  });
  const s = params.toString();
  return s ? `?${s}` : '';
}

export const DocumentsApi = {
  list: (filters: DocumentFilters = {}) =>
    api.get<PagedList<DocumentDto>>(`/documents${qs(filters)}`),

  get: (id: string) => api.get<DocumentDto>(`/documents/${id}`),

  upload: (input: UploadDocumentInput) => {
    const form = new FormData();
    form.append('file', input.file);
    form.append('documentType', input.documentType ?? 'Other');
    if (input.issuingAuthority) form.append('issuingAuthority', input.issuingAuthority);
    if (input.financialYear) form.append('financialYear', input.financialYear);
    if (input.notes) form.append('notes', input.notes);
    if (input.issueDate) form.append('issueDate', input.issueDate);
    if (input.expiryDate) form.append('expiryDate', input.expiryDate);
    if (input.folderId) form.append('folderId', input.folderId);
    if (input.projectId) form.append('projectId', input.projectId);
    if (input.tags) form.append('tags', input.tags);
    return api.upload<DocumentDto>('/documents/upload', form);
  },

  remove: (id: string) => api.del<void>(`/documents/${id}`),

  update: (id: string, input: Partial<{
    name: string; documentType: string; issuingAuthority: string; financialYear: string;
    notes: string; issueDate: string; expiryDate: string; folderId: string; tags: string[];
  }>) => {
    // Send documentType as its numeric enum value. Integers bind to the backend enum even when
    // the API isn't configured with a string-enum converter, so categorization is reliable.
    const body: Record<string, unknown> = { ...input };
    if (input.documentType !== undefined) body.documentType = DOCUMENT_TYPE_INT[input.documentType] ?? 99;
    return api.put<DocumentDto>(`/documents/${id}`, body);
  },

  downloadUrl: (id: string) => `/documents/${id}/download`,
  download: (id: string) => api.blob(`/documents/${id}/download`),

  approve: (id: string) => api.post<DocumentDto>(`/documents/${id}/approve`),
  reject: (id: string, reason?: string) => api.post<DocumentDto>(`/documents/${id}/reject`, { reason }),
  requestChanges: (id: string, comment?: string) =>
    api.post<DocumentDto>(`/documents/${id}/request-changes`, { comment }),
};
