import type {
  DocumentItem, ProjectItem, FolderNode, NotificationItem, TeamMember, CurrentUser,
} from '@/types';
import {
  currentUser, seedDocuments, seedProjects, seedFolders, seedNotifications, seedTeam,
} from './seed';

/** Service contracts. Implement these against the .NET Web API to go live. */
export interface IDocumentService {
  list(): Promise<DocumentItem[]>;
  upload(input: { name: string; type?: string; authority?: string; folderId?: string | null; sizeKb?: number }): Promise<DocumentItem>;
  remove(id: string): Promise<void>;
}
export interface IProjectService {
  list(): Promise<ProjectItem[]>;
  get(id: string): Promise<ProjectItem | undefined>;
  create(input: { name: string; description?: string }): Promise<ProjectItem>;
  update(input: { id: string; name?: string; description?: string }): Promise<ProjectItem>;
  remove(id: string): Promise<void>;
  setDocuments(id: string, documentIds: string[]): Promise<ProjectItem>;
}
export interface IFolderService {
  list(): Promise<FolderNode[]>;
}
export interface INotificationService {
  list(): Promise<NotificationItem[]>;
  markAllRead(): Promise<void>;
}
export interface ITeamService {
  list(): Promise<TeamMember[]>;
  me(): Promise<CurrentUser>;
}

const clone = <T,>(v: T): T => JSON.parse(JSON.stringify(v));
const latency = (ms = 160) => new Promise((r) => setTimeout(r, ms));

let documents = clone(seedDocuments);
let projects = clone(seedProjects);
const folders = clone(seedFolders);
let notifications = clone(seedNotifications);
const team = clone(seedTeam);

export const documentService: IDocumentService = {
  async list() { await latency(); return clone(documents); },
  async upload(input) {
    await latency(220);
    const doc: DocumentItem = {
      id: `d-${Date.now()}`,
      name: input.name,
      type: input.type ?? 'Additional Documents',
      authority: input.authority ?? 'Internal',
      financialYear: 'FY 2025-26',
      tags: [],
      status: 'valid',
      approval: 'pending',
      uploadedAt: new Date().toISOString(),
      uploader: currentUser.name,
      sizeKb: input.sizeKb ?? 200,
      folderId: input.folderId ?? null,
    };
    documents = [doc, ...documents];
    return clone(doc);
  },
  async remove(id) { await latency(); documents = documents.filter((d) => d.id !== id); },
};

export const projectService: IProjectService = {
  async list() { await latency(); return clone(projects); },
  async get(id) { await latency(); return clone(projects.find((p) => p.id === id)); },
  async create(input) {
    await latency(220);
    const p: ProjectItem = {
      id: `p-${Date.now()}`, name: input.name, description: input.description ?? '',
      documentIds: [], createdAt: new Date().toISOString(),
    };
    projects = [p, ...projects];
    return clone(p);
  },
  async update(input) {
    await latency(160);
    projects = projects.map((p) => (p.id === input.id
      ? { ...p, name: input.name ?? p.name, description: input.description ?? p.description }
      : p));
    return clone(projects.find((p) => p.id === input.id)!);
  },
  async remove(id) { await latency(); projects = projects.filter((p) => p.id !== id); },
  async setDocuments(id, documentIds) {
    await latency(120);
    projects = projects.map((p) => (p.id === id ? { ...p, documentIds } : p));
    return clone(projects.find((p) => p.id === id)!);
  },
};

export const folderService: IFolderService = {
  async list() { await latency(); return clone(folders); },
};

export const notificationService: INotificationService = {
  async list() { await latency(); return clone(notifications); },
  async markAllRead() { await latency(); notifications = notifications.map((n) => ({ ...n, read: true })); },
};

export const teamService: ITeamService = {
  async list() { await latency(); return clone(team); },
  async me() { await latency(); return { ...currentUser }; },
};
