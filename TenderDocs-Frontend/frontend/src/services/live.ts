/**
 * Live data services backed by the ASP.NET Core API.
 *
 * These implement the same I*Service contracts the app already consumes (see ./store),
 * translating backend DTOs into the frontend's view types. Swapping these in (via
 * ./index) makes every existing screen run on real backend data with no component changes.
 */
import type {
  DocumentItem, ProjectItem, FolderNode, NotificationItem, TeamMember, CurrentUser, ExpiryStatus,
} from '@/types';
import { normalizeRole, normalizeApproval } from '@/types';
import type {
  IDocumentService, IProjectService, IFolderService, INotificationService, ITeamService,
} from './store';
import {
  DocumentsApi, ProjectsApi, FoldersApi, NotificationsApi, AuthApi, UsersApi,
} from './api';
import type {
  DocumentDto, ProjectSummaryDto, ProjectDetailDto, FolderNodeDto, NotificationDto,
  TeamMemberDto, UserDto,
} from './api';

// ---- mappers --------------------------------------------------------------

function mapStatus(status: string): ExpiryStatus {
  switch (status) {
    case 'Valid': return 'valid';
    case 'ExpiringSoon': return 'expiring';
    case 'Expired': return 'expired';
    default: return 'none';
  }
}

function mapDocument(d: DocumentDto): DocumentItem {
  return {
    id: d.id,
    name: d.name,
    type: d.documentTypeLabel || d.documentType,
    category: d.documentType,
    authority: d.issuingAuthority ?? '—',
    financialYear: d.financialYear ?? '—',
    tags: d.tags ?? [],
    status: mapStatus(d.status),
    approval: normalizeApproval(d.approvalStatus),
    approvedBy: d.approvedByName ?? null,
    rejectionReason: d.rejectionReason ?? null,
    expiryDate: d.expiryDate ?? null,
    notes: d.notes ?? '',
    contentType: d.contentType,
    uploadedAt: d.createdAt,
    uploader: d.uploadedByName ?? 'Unknown',
    sizeKb: Math.max(1, Math.round(d.fileSizeBytes / 1024)),
    folderId: d.folderId ?? null,
  };
}

function mapProjectSummary(p: ProjectSummaryDto): ProjectItem {
  return {
    id: p.id,
    name: p.name,
    description: p.description ?? '',
    documentIds: p.documentIds ?? [],
    createdAt: p.createdAt,
  };
}

function mapProjectDetail(p: ProjectDetailDto): ProjectItem {
  return {
    id: p.id,
    name: p.name,
    description: p.description ?? '',
    documentIds: (p.documents ?? []).map((d) => d.id),
    createdAt: p.createdAt,
  };
}

function flattenFolders(nodes: FolderNodeDto[]): FolderNode[] {
  const out: FolderNode[] = [];
  const walk = (list: FolderNodeDto[]) => {
    for (const n of list) {
      out.push({ id: n.id, name: n.name, parentId: n.parentFolderId ?? null });
      if (n.children?.length) walk(n.children);
    }
  };
  walk(nodes);
  return out;
}

function mapNotificationKind(type: string): NotificationItem['kind'] {
  switch (type) {
    case 'DocumentExpiring':
    case 'DocumentExpired': return 'expiry';
    case 'DocumentUploaded': return 'upload';
    case 'ProjectShared': return 'project';
    default: return 'system';
  }
}

function mapNotification(n: NotificationDto): NotificationItem {
  return {
    id: n.id,
    title: n.title,
    body: n.message,
    kind: mapNotificationKind(n.type),
    read: n.isRead,
    createdAt: n.createdAt,
  };
}

function mapRole(role: string): TeamMember['role'] {
  switch (role) {
    case 'Admin': return 'Admin';
    case 'Manager': return 'Editor';
    case 'Viewer': return 'Viewer';
    default: return 'Viewer';
  }
}

function mapTeamMember(u: TeamMemberDto): TeamMember {
  return {
    id: u.id,
    name: u.fullName,
    email: u.email,
    role: mapRole(u.role),
    initials: u.initials,
    status: u.isActive ? 'active' : 'invited',
  };
}

function mapMe(u: UserDto): CurrentUser {
  return { name: u.fullName, initials: u.initials, email: u.email, role: normalizeRole(u.role) };
}

// ---- services -------------------------------------------------------------

export const documentService: IDocumentService = {
  async list() {
    const page = await DocumentsApi.list({ page: 1, pageSize: 200 });
    return page.items.map(mapDocument);
  },
  async upload(input: {
    name: string; type?: string; authority?: string; folderId?: string | null;
    sizeKb?: number; file?: File;
  }) {
    if (!input.file) {
      throw new Error('A file is required to upload a document.');
    }
    const dto = await DocumentsApi.upload({
      file: input.file,
      documentType: input.type,
      issuingAuthority: input.authority,
      folderId: input.folderId ?? undefined,
    });
    return mapDocument(dto);
  },
  async remove(id) {
    await DocumentsApi.remove(id);
  },
};

export const projectService: IProjectService = {
  async list() {
    const projects = await ProjectsApi.summary();
    return projects.map(mapProjectSummary);
  },
  async get(id) {
    const detail = await ProjectsApi.get(id);
    return mapProjectDetail(detail);
  },
  async create(input) {
    const dto = await ProjectsApi.create(input.name, input.description);
    return { id: dto.id, name: dto.name, description: dto.description ?? '', documentIds: [], createdAt: dto.createdAt };
  },
  async update(input) {
    const dto = await ProjectsApi.update(input.id, { name: input.name, description: input.description });
    return { id: dto.id, name: dto.name, description: dto.description ?? '', documentIds: [], createdAt: dto.createdAt };
  },
  async remove(id) {
    await ProjectsApi.remove(id);
  },
  async setDocuments(id, documentIds) {
    const detail = await ProjectsApi.setDocuments(id, documentIds);
    return mapProjectDetail(detail);
  },
};

export const folderService: IFolderService = {
  async list() {
    const tree = await FoldersApi.tree();
    return flattenFolders(tree);
  },
};

export const notificationService: INotificationService = {
  async list() {
    const items = await NotificationsApi.list();
    return items.map(mapNotification);
  },
  async markAllRead() {
    await NotificationsApi.markAllRead();
  },
};

export const teamService: ITeamService = {
  async list() {
    const users = await UsersApi.list();
    return users.map(mapTeamMember);
  },
  async me() {
    const user = await AuthApi.me();
    return mapMe(user);
  },
};
