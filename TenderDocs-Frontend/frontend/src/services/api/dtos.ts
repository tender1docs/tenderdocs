/** Backend DTO shapes (mirror the ASP.NET Core API responses). */

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  initials: string;
  role: string;            // Admin | Approver | Uploader | Viewer
  organizationId: string;
  organizationName: string;
  demoMode: boolean;
  permissions: string[];   // resolved permission keys for the signed-in user
}

export interface AuthResultDto {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  user: UserDto;
}

export interface DocumentDto {
  id: string;
  name: string;
  documentType: string;        // enum name (Gst, Pan, …)
  documentTypeLabel: string;   // "GST Certificate"
  issuingAuthority: string | null;
  financialYear: string | null;
  notes: string | null;
  issueDate: string | null;
  expiryDate: string | null;
  status: string;              // Valid | ExpiringSoon | Expired | NoExpiry
  storageProvider: string;
  fileSizeBytes: number;
  contentType: string;
  folderId: string | null;
  uploadedById: string | null;
  uploadedByName: string | null;
  createdAt: string;
  tags: string[];
  approvalStatus: string;          // "Pending" | "Approved" | "Rejected"
  approvedByName: string | null;
  approvalAt: string | null;
  rejectionReason: string | null;
}

export interface PagedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ProjectDto {
  id: string;
  name: string;
  description: string | null;
  documentCount: number;
  createdAt: string;
  createdById: string | null;
}

export interface ProjectSummaryDto {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
  createdById: string | null;
  documentIds: string[];
}

export interface ProjectRequirementDto {
  id: string;
  name: string;
  description: string | null;
  isMandatory: boolean;
  sortOrder: number;
  categoryId: string | null;
  fulfilledByDocumentId: string | null;
}

/** A top-level Organize category with its ordered sub-category rows. */
export interface ProjectRequirementCategoryDto {
  id: string;
  name: string;
  sortOrder: number;
  requirements: ProjectRequirementDto[];
}

export interface ProjectAssignmentDto {
  documentId: string;
  requirementId: string | null;
}

export interface ProjectDetailDto {
  id: string;
  name: string;
  description: string | null;
  documentCount: number;
  createdAt: string;
  documents: DocumentDto[];
  categories: ProjectRequirementCategoryDto[];
  requirements: ProjectRequirementDto[];
  assignments: ProjectAssignmentDto[];
}

export interface FolderNodeDto {
  id: string;
  name: string;
  parentFolderId: string | null;
  depth: number;
  materializedPath: string;
  documentCount: number;
  children: FolderNodeDto[];
}

export interface NotificationDto {
  id: string;
  type: string;            // DocumentExpiring | DocumentExpired | DocumentUploaded | ProjectShared | System
  title: string;
  message: string;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface TeamMemberDto {
  id: string;
  fullName: string;
  email: string;
  role: string;            // Admin | Approver | Uploader | Viewer
  initials: string;
  isActive: boolean;
}

export interface StorageStatusDto {
  activeProvider: string;
  googleDriveConnected: boolean;
  googleDriveFolderId: string | null;
}

export interface SearchProjectHit {
  id: string;
  name: string;
  description: string | null;
}

export interface GlobalSearchResultDto {
  documents: DocumentDto[];
  projects: SearchProjectHit[];
}
