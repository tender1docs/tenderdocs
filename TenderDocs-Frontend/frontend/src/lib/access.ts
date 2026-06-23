import type { Role } from '@/types';

/** Action-level permissions, independent of which page you're on. */
export type Action =
  | 'upload'         // upload / add documents
  | 'approve'        // approve / reject documents
  | 'organizeEdit'   // edit the Organize structure (add/rename/delete categories & rows, assign)
  | 'deleteDoc'      // delete documents / remove from project
  | 'manageProject'  // create projects, add documents to a project
  | 'settings';      // settings / storage / team

/** Top-level pages each role may visit. Organize is reached through project pages. */
const PAGES: Record<Role, string[]> = {
  approver: ['/dashboard', '/documents', '/projects', '/organize', '/notifications', '/team', '/settings'],
  uploader: ['/dashboard', '/documents', '/projects'],
  viewer:   ['/dashboard', '/documents', '/projects', '/organize'],
};

const ACTIONS: Record<Role, Action[]> = {
  approver: ['upload', 'approve', 'organizeEdit', 'deleteDoc', 'manageProject', 'settings'],
  uploader: ['upload', 'manageProject'],
  viewer:   [],
};

export function pagesFor(role: Role): string[] {
  return PAGES[role];
}

export function can(role: Role, action: Action): boolean {
  return ACTIONS[role].includes(action);
}

/** Whether a role may visit a given pathname (project organize requires '/organize' access). */
export function canVisit(role: Role, pathname: string): boolean {
  const allowed = PAGES[role];
  if (/^\/projects\/[^/]+\/organize/.test(pathname)) return allowed.includes('/organize');
  if (pathname.startsWith('/projects')) return allowed.includes('/projects');
  const seg = '/' + (pathname.split('/')[1] ?? '');
  return allowed.includes(seg);
}
