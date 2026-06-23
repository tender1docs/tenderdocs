import type {
  DocumentItem, ProjectItem, FolderNode, NotificationItem, TeamMember, CurrentUser,
} from '@/types';

export const currentUser: CurrentUser = {
  name: 'Prasad',
  initials: 'PG',
  email: 'prasad@tenderdocs.io',
  role: 'approver',
};

// ---- Folder tree (Google Drive style) ----
export const seedFolders: FolderNode[] = [
  { id: 'f-org', name: 'XYZ Company', parentId: null },
  { id: 'f-fin', name: 'Finance', parentId: 'f-org' },
  { id: 'f-tax', name: 'Tax', parentId: 'f-fin' },
  { id: 'f-gst', name: 'GST', parentId: 'f-tax' },
  { id: 'f-itr', name: 'IT Returns', parentId: 'f-tax' },
  { id: 'f-pan', name: 'PAN', parentId: 'f-tax' },
  { id: 'f-bank', name: 'Bank Statements', parentId: 'f-fin' },
  { id: 'f-legal', name: 'Legal & Compliance', parentId: 'f-org' },
  { id: 'f-iso', name: 'ISO', parentId: 'f-legal' },
  { id: 'f-msme', name: 'MSME', parentId: 'f-legal' },
  { id: 'f-tech', name: 'Technical', parentId: 'f-org' },
  { id: 'f-exp', name: 'Experience Certificates', parentId: 'f-tech' },
];

function iso(d: string) { return new Date(d).toISOString(); }

const baseDocuments: Omit<DocumentItem, 'approval'>[] = [
  {
    id: 'd-1', name: 'sample.pdf', type: 'GST Certificate', authority: 'GST Department',
    financialYear: 'FY 2024-25', tags: ['GST Department', 'FY 2024-25', 'urgent', 'fy2025'],
    status: 'valid', uploadedAt: iso('2026-05-19'), uploader: 'Prasad', sizeKb: 248, folderId: 'f-gst',
  },
  {
    id: 'd-2', name: 'GST-Registration-2025.pdf', type: 'GST Certificate', authority: 'GST Department',
    financialYear: 'FY 2025-26', tags: ['GST', 'registration'], status: 'valid',
    uploadedAt: iso('2026-04-02'), uploader: 'Prasad', sizeKb: 312, folderId: 'f-gst',
  },
  {
    id: 'd-3', name: 'ITR-Acknowledgement-FY24.pdf', type: 'IT Returns', authority: 'Income Tax Dept',
    financialYear: 'FY 2024-25', tags: ['ITR', 'income tax'], status: 'valid',
    uploadedAt: iso('2026-03-15'), uploader: 'Anita', sizeKb: 540, folderId: 'f-itr',
  },
  {
    id: 'd-4', name: 'PAN-Card.pdf', type: 'PAN', authority: 'Income Tax Dept',
    financialYear: '—', tags: ['PAN', 'identity'], status: 'none',
    uploadedAt: iso('2026-02-20'), uploader: 'Prasad', sizeKb: 96, folderId: 'f-pan',
  },
  {
    id: 'd-5', name: 'MSME-Udyam-Certificate.pdf', type: 'MSME', authority: 'Ministry of MSME',
    financialYear: '—', tags: ['MSME', 'Udyam'], status: 'valid',
    uploadedAt: iso('2026-01-28'), uploader: 'Anita', sizeKb: 180, folderId: 'f-msme',
  },
  {
    id: 'd-6', name: 'ISO-9001-2015.pdf', type: 'ISO', authority: 'Bureau Veritas',
    financialYear: '—', tags: ['ISO', 'quality'], status: 'expiring',
    uploadedAt: iso('2025-12-10'), uploader: 'Ravi', sizeKb: 420, folderId: 'f-iso',
  },
  {
    id: 'd-7', name: 'Experience-Certificate-NHAI.pdf', type: 'Experience Certificates', authority: 'NHAI',
    financialYear: 'FY 2023-24', tags: ['experience', 'roads'], status: 'valid',
    uploadedAt: iso('2025-11-05'), uploader: 'Ravi', sizeKb: 288, folderId: 'f-exp',
  },
  {
    id: 'd-8', name: 'Bank-Statement-Q4.pdf', type: 'Bank Statements', authority: 'HDFC Bank',
    financialYear: 'FY 2024-25', tags: ['bank', 'financial'], status: 'valid',
    uploadedAt: iso('2026-04-30'), uploader: 'Prasad', sizeKb: 760, folderId: 'f-bank',
  },
  {
    id: 'd-9', name: 'Technical-Capability-Statement.pdf', type: 'Technical Documents', authority: 'Internal',
    financialYear: '—', tags: ['technical', 'capability'], status: 'none',
    uploadedAt: iso('2026-05-08'), uploader: 'Anita', sizeKb: 1024, folderId: 'f-tech',
  },
  {
    id: 'd-10', name: 'Audited-Financials-FY24.pdf', type: 'Financial Documents', authority: 'KPMG',
    financialYear: 'FY 2024-25', tags: ['financial', 'audit'], status: 'valid',
    uploadedAt: iso('2026-03-22'), uploader: 'Ravi', sizeKb: 1340, folderId: 'f-fin',
  },
  {
    id: 'd-11', name: 'EMD-DemandDraft.pdf', type: 'EMD', authority: 'SBI',
    financialYear: 'FY 2025-26', tags: ['EMD', 'deposit'], status: 'expiring',
    uploadedAt: iso('2026-05-12'), uploader: 'Prasad', sizeKb: 64, folderId: 'f-fin',
  },
  {
    id: 'd-12', name: 'Power-of-Attorney.pdf', type: 'Additional Documents', authority: 'Notary',
    financialYear: '—', tags: ['legal', 'authorization'], status: 'expired',
    uploadedAt: iso('2025-09-01'), uploader: 'Anita', sizeKb: 210, folderId: 'f-legal',
  },
];

export const seedDocuments: DocumentItem[] = baseDocuments.map((d, i) => ({
  ...d, approval: (['approved', 'pending', 'rejected'] as const)[i % 3],
}));

export const seedProjects: ProjectItem[] = [
  { id: 'p-1', name: 'IPHONE SUPPLY', description: '', documentIds: ['d-1'], createdAt: iso('2026-06-15') },
  { id: 'p-2', name: 'ROAD CONSTRUCTION', description: 'Highway resurfacing tender', documentIds: ['d-7', 'd-3'], createdAt: iso('2026-06-10') },
  { id: 'p-3', name: 'METRO PROJECT', description: 'Phase II metro civil works', documentIds: ['d-5', 'd-6', 'd-10'], createdAt: iso('2026-06-04') },
  { id: 'p-4', name: 'SMART CITY PROJECT', description: 'Urban infrastructure package', documentIds: ['d-9', 'd-8'], createdAt: iso('2026-05-28') },
  { id: 'p-5', name: 'test', description: '', documentIds: ['d-2'], createdAt: iso('2026-05-19') },
  { id: 'p-6', name: 'NHAI Bridge 2025', description: 'Demo tender bundle', documentIds: ['d-7'], createdAt: iso('2026-05-19') },
];

// Default requirement names used when a fresh Organize workspace opens
export const requirementTemplate: string[] = [
  'GST', 'IT Returns', 'PAN', 'MSME', 'ISO', 'Experience Certificates',
  'Bank Statements', 'Technical Documents', 'Financial Documents', 'EMD', 'Additional Documents',
];

export const seedNotifications: NotificationItem[] = [
  { id: 'n-1', title: 'ISO 9001 expiring soon', body: 'ISO-9001-2015.pdf expires in 22 days. Renew to keep tenders valid.', kind: 'expiry', read: false, createdAt: iso('2026-06-14') },
  { id: 'n-2', title: 'EMD draft expiring', body: 'EMD-DemandDraft.pdf is approaching its validity window.', kind: 'expiry', read: false, createdAt: iso('2026-06-13') },
  { id: 'n-3', title: 'Project package ready', body: 'METRO PROJECT package was generated and is ready to download.', kind: 'project', read: false, createdAt: iso('2026-06-12') },
  { id: 'n-4', title: 'New document uploaded', body: 'Anita uploaded Technical-Capability-Statement.pdf.', kind: 'upload', read: true, createdAt: iso('2026-06-08') },
  { id: 'n-5', title: 'Power of Attorney expired', body: 'Power-of-Attorney.pdf has expired. Replace it before submission.', kind: 'expiry', read: true, createdAt: iso('2026-06-01') },
];

export const seedTeam: TeamMember[] = [
  { id: 't-1', name: 'Prasad', email: 'prasad@tenderdocs.io', role: 'Owner', initials: 'PG', status: 'active' },
  { id: 't-2', name: 'Anita Rao', email: 'anita@tenderdocs.io', role: 'Admin', initials: 'AR', status: 'active' },
  { id: 't-3', name: 'Ravi Menon', email: 'ravi@tenderdocs.io', role: 'Editor', initials: 'RM', status: 'active' },
  { id: 't-4', name: 'Sara Joseph', email: 'sara@tenderdocs.io', role: 'Viewer', initials: 'SJ', status: 'invited' },
];
