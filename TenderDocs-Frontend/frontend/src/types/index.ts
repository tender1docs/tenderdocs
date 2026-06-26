export type ExpiryStatus = 'valid' | 'expiring' | 'expired' | 'none';

/**
 * Access roles. admin = full access (the only superuser), approver = approval only,
 * uploader = document work only, viewer = read-only. Roles map to permission sets on the backend;
 * the frontend gates UI from the resolved permission list (see lib/access.ts).
 */
export type Role = 'admin' | 'approver' | 'uploader' | 'viewer';

/** Document review state set by an approver. */
export type ApprovalStatus = 'pending' | 'approved' | 'rejected';

/** Map a backend role string ("Admin" | "Approver" | "Uploader" | "Viewer") to the frontend Role. */
export function normalizeRole(role?: string | null): Role {
  switch ((role ?? '').toLowerCase()) {
    case 'admin': return 'admin';
    case 'approver': return 'approver';
    case 'uploader': return 'uploader';
    default: return 'viewer';
  }
}

export function normalizeApproval(status?: string | null): ApprovalStatus {
  switch ((status ?? '').toLowerCase()) {
    case 'approved': return 'approved';
    case 'rejected': return 'rejected';
    default: return 'pending';
  }
}

export interface DocumentItem {
  id: string;
  name: string;
  type: string;            // display label, e.g. "GST"
  category?: string;       // backend enum name, e.g. "Gst" (drives upload + organize)
  authority: string;       // e.g. "GST Department"
  financialYear: string;   // e.g. "FY 2024-25"
  tags: string[];
  status: ExpiryStatus;
  approval: ApprovalStatus;
  approvedBy?: string | null;
  rejectionReason?: string | null;
  expiryDate?: string | null;
  notes?: string;
  contentType?: string;
  uploadedAt: string;      // ISO
  uploader: string;
  sizeKb: number;
  folderId: string | null;
}

/** Requirement categories shown across upload, filters and organize. */
export const DOCUMENT_CATEGORIES: { value: string; label: string }[] = [
  { value: 'Gst', label: 'GST' },
  { value: 'Pan', label: 'PAN' },
  { value: 'Itr', label: 'IT Returns' },
  { value: 'Msme', label: 'MSME' },
  { value: 'Iso', label: 'ISO' },
  { value: 'ExperienceCertificate', label: 'Experience Certificates' },
  { value: 'BankStatement', label: 'Bank Statements' },
  { value: 'FinancialDocument', label: 'Financial Documents' },
  { value: 'TechnicalDocument', label: 'Technical Documents' },
  { value: 'Other', label: 'Others' },
];

export interface ProjectItem {
  id: string;
  name: string;
  description: string;
  documentIds: string[];
  createdAt: string;       // ISO
}

export interface FolderNode {
  id: string;
  name: string;
  parentId: string | null;
}

export interface RequirementRow {
  id: string;
  name: string;
  documentIds: string[];   // linked documents
}

export interface NotificationItem {
  id: string;
  title: string;
  body: string;
  kind: 'expiry' | 'project' | 'system' | 'upload';
  read: boolean;
  createdAt: string;
}

export interface TeamMember {
  id: string;
  name: string;
  email: string;
  role: 'Owner' | 'Admin' | 'Editor' | 'Viewer';
  initials: string;
  status: 'active' | 'invited';
}

export interface CurrentUser {
  name: string;
  initials: string;
  email: string;
  role: Role;
}
