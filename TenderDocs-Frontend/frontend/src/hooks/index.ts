import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useState, createContext, useContext } from 'react';
import {
  documentService, projectService, folderService, notificationService, teamService,
} from '@/services';
import { OrganizeApi } from '@/services/api';

export function useDocuments() {
  return useQuery({ queryKey: ['documents'], queryFn: () => documentService.list() });
}
export function useProjects() {
  return useQuery({ queryKey: ['projects'], queryFn: () => projectService.list() });
}
export function useProject(id?: string) {
  return useQuery({ queryKey: ['project', id], queryFn: () => projectService.get(id!), enabled: !!id });
}
/**
 * Raw project detail for the Organize workspace: includes the editable category/row structure and
 * document→row assignments that the trimmed-down `useProject` mapping drops.
 */
export function useOrganizeDetail(id?: string) {
  return useQuery({ queryKey: ['organize', id], queryFn: () => OrganizeApi.project(id!), enabled: !!id });
}
export function useFolders() {
  return useQuery({ queryKey: ['folders'], queryFn: () => folderService.list() });
}
export function useNotifications() {
  return useQuery({ queryKey: ['notifications'], queryFn: () => notificationService.list() });
}
export function useTeam() {
  return useQuery({ queryKey: ['team'], queryFn: () => teamService.list() });
}
export function useMe() {
  return useQuery({ queryKey: ['me'], queryFn: () => teamService.me() });
}

export function useUploadDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { name: string; sizeKb: number; type?: string; authority?: string; folderId?: string | null; file?: File }) =>
      documentService.upload(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['documents'] }),
  });
}
export function useDeleteDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => documentService.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['documents'] }),
  });
}
export function useCreateProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { name: string; description?: string }) => projectService.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['projects'] }),
  });
}
export function useUpdateProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { id: string; name?: string; description?: string }) => projectService.update(input),
    onSuccess: (_d, v) => {
      qc.invalidateQueries({ queryKey: ['projects'] });
      qc.invalidateQueries({ queryKey: ['project', v.id] });
    },
  });
}
export function useDeleteProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => projectService.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['projects'] }),
  });
}
export function useSetProjectDocuments() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { id: string; documentIds: string[] }) =>
      projectService.setDocuments(input.id, input.documentIds),
    onSuccess: (_d, v) => {
      qc.invalidateQueries({ queryKey: ['projects'] });
      qc.invalidateQueries({ queryKey: ['project', v.id] });
    },
  });
}

// ---- Theme ----
type Theme = 'light' | 'dark';
export function useTheme() {
  const [theme, setTheme] = useState<Theme>(() => {
    if (typeof window === 'undefined') return 'light';
    const stored = localStorage.getItem('td-theme') as Theme | null;
    if (stored) return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  });
  useEffect(() => {
    const root = document.documentElement;
    root.classList.toggle('dark', theme === 'dark');
    localStorage.setItem('td-theme', theme);
  }, [theme]);
  const toggle = useCallback(() => setTheme((t) => (t === 'light' ? 'dark' : 'light')), []);
  return { theme, setTheme, toggle };
}

export function useMediaQuery(query: string) {
  const [matches, setMatches] = useState(() =>
    typeof window !== 'undefined' ? window.matchMedia(query).matches : false,
  );
  useEffect(() => {
    const m = window.matchMedia(query);
    const fn = () => setMatches(m.matches);
    fn();
    m.addEventListener('change', fn);
    return () => m.removeEventListener('change', fn);
  }, [query]);
  return matches;
}

// ---- Toast ----
export interface Toast { id: number; title: string; tone?: 'default' | 'success' | 'danger' }
interface ToastCtx { toasts: Toast[]; push: (t: Omit<Toast, 'id'>) => void; dismiss: (id: number) => void }
export const ToastContext = createContext<ToastCtx | null>(null);
export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within ToastProvider');
  return ctx;
}
