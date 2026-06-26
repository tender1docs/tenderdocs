import { FileType2, MoreVertical, Trash2, Download, Eye, FolderPlus, X, CheckCircle2, Building2, CalendarDays, Layers3 } from 'lucide-react';
import { useEffect, useState, type MouseEvent } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { useQueryClient } from '@tanstack/react-query';
import { Badge, StatusBadge, ApprovalBadge, Modal, Button, EmptyState } from '@/components/ui';
import { useToast, useProjects } from '@/hooks';
import { useAuth } from '@/auth/AuthProvider';
import { can, Permission } from '@/lib/access';
import { apiClients, saveBlob } from '@/services';
import { fmtDayMonth } from '@/lib/utils';
import { useDocumentDrawer } from '@/features/documents/DocumentDrawer';
import type { DocumentItem } from '@/types';

async function downloadDocument(doc: DocumentItem): Promise<void> {
  saveBlob(await apiClients.DocumentsApi.download(doc.id), doc.name);
}

/* number of projects a document is used in */
function useProjectCount(docId: string): number {
  const { data: projects = [] } = useProjects();
  return projects.filter((p) => p.documentIds.includes(docId)).length;
}

/* -------------------------- Add To Project modal -------------------------- */
function AddToProjectModal({ doc, open, onClose }: { doc: DocumentItem; open: boolean; onClose: () => void }) {
  const { data: projects = [] } = useProjects();
  const { push } = useToast();
  const qc = useQueryClient();
  const [busyId, setBusyId] = useState<string | null>(null);
  const [done, setDone] = useState<Set<string>>(new Set());
  useEffect(() => { if (open) setDone(new Set()); }, [open]);

  async function add(projectId: string) {
    setBusyId(projectId);
    try {
      await apiClients.ProjectsApi.addDocument(projectId, doc.id);
      await qc.invalidateQueries({ queryKey: ['projects'] });
      await qc.invalidateQueries({ queryKey: ['project', projectId] });
      setDone((p) => new Set(p).add(projectId));
      push({ title: 'Added to project', tone: 'success' });
    } catch (err) { push({ title: (err as Error)?.message || 'Could not add', tone: 'danger' }); }
    finally { setBusyId(null); }
  }

  return (
    <Modal open={open} onClose={onClose} title="Add to project" width="max-w-lg">
      <div className="mb-3 flex items-center gap-2.5 rounded-xl border border-line bg-slate-50/60 p-3 dark:border-[#222A31] dark:bg-[#12181D]">
        <FileType2 className="h-5 w-5 text-pdf" />
        <div className="min-w-0"><p className="truncate text-sm font-medium text-ink dark:text-slate-100">{doc.name}</p><p className="truncate text-xs text-ink-muted">{doc.type}</p></div>
      </div>
      <div className="max-h-80 space-y-2 overflow-y-auto">
        {projects.length === 0 ? <EmptyState icon={<FolderPlus className="h-5 w-5" />} title="No projects yet" hint="Create a project first." /> :
          projects.map((p) => {
            const added = done.has(p.id) || p.documentIds.includes(doc.id);
            return (
              <div key={p.id} className="flex items-center gap-3 rounded-xl border border-line p-3 dark:border-[#1F262C]">
                <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-50 text-brand-600 dark:bg-brand-900/40"><FolderPlus className="h-4 w-4" /></span>
                <div className="min-w-0 flex-1"><p className="truncate text-sm font-medium text-ink dark:text-slate-100">{p.name}</p><p className="truncate text-xs text-ink-muted">{p.documentIds.length} documents</p></div>
                {added ? <span className="inline-flex items-center gap-1 text-xs font-medium text-valid-text"><CheckCircle2 className="h-4 w-4" /> Added</span>
                  : <Button size="sm" variant="soft" loading={busyId === p.id} onClick={() => add(p.id)}>Add</Button>}
              </div>
            );
          })}
      </div>
    </Modal>
  );
}

/* -------------------------------- Menu ----------------------------------- */
function Menu({ doc, onDelete, onView, onAdd }: { doc: DocumentItem; onDelete?: () => void; onView: () => void; onAdd?: () => void }) {
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const { push } = useToast();

  async function handleDownload(e: MouseEvent) {
    e.stopPropagation(); setOpen(false);
    if (busy) return; setBusy(true);
    try { await downloadDocument(doc); } catch { push({ title: 'Download failed', tone: 'danger' }); } finally { setBusy(false); }
  }

  return (
    <div className="relative shrink-0" onClick={(e) => e.stopPropagation()}>
      <button onClick={(e) => { e.stopPropagation(); setOpen((o) => !o); }} className="rounded-lg p-1.5 text-ink-faint hover:bg-slate-100 dark:hover:bg-[#1B232A]"><MoreVertical className="h-4 w-4" /></button>
      <AnimatePresence>
        {open && (
          <>
            <div className="fixed inset-0 z-10" onClick={(e) => { e.stopPropagation(); setOpen(false); }} />
            <motion.div initial={{ opacity: 0, scale: 0.96, y: -4 }} animate={{ opacity: 1, scale: 1, y: 0 }} exit={{ opacity: 0 }}
              className="absolute right-0 z-20 mt-1 w-44 overflow-hidden rounded-xl border border-line bg-white py-1 shadow-lift dark:border-[#222A31] dark:bg-[#12181D]">
              <button onClick={(e) => { e.stopPropagation(); setOpen(false); onView(); }} className="flex w-full items-center gap-2 px-3 py-2 text-sm text-ink-soft hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-[#161D23]"><Eye className="h-4 w-4" /> View details</button>
              <button onClick={handleDownload} disabled={busy} className="flex w-full items-center gap-2 px-3 py-2 text-sm text-ink-soft hover:bg-slate-50 disabled:opacity-60 dark:text-slate-300 dark:hover:bg-[#161D23]"><Download className="h-4 w-4" /> {busy ? 'Downloading…' : 'Download'}</button>
              {onAdd && <button onClick={(e) => { e.stopPropagation(); setOpen(false); onAdd(); }} className="flex w-full items-center gap-2 px-3 py-2 text-sm text-ink-soft hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-[#161D23]"><FolderPlus className="h-4 w-4" /> Add To Project</button>}
              {onDelete && <button onClick={(e) => { e.stopPropagation(); setOpen(false); onDelete(); }} className="flex w-full items-center gap-2 px-3 py-2 text-sm text-danger-text hover:bg-danger-bg"><Trash2 className="h-4 w-4" /> Delete</button>}
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </div>
  );
}

export function DocumentCard({ doc, onDelete }: { doc: DocumentItem; onDelete?: () => void }) {
  const { open } = useDocumentDrawer();
  const { permissions } = useAuth();
  const canAdd = can(permissions, Permission.ProjectsAssign);
  const [addOpen, setAddOpen] = useState(false);
  const projectCount = useProjectCount(doc.id);
  const userTags = (doc.tags ?? []).filter((t) => !t.startsWith('ref:') && !t.startsWith('cat:') && !t.startsWith('desc:'));

  return (
    <motion.div layout initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, scale: 0.97 }}
      onClick={() => open(doc)} role="button"
      className="surface flex cursor-pointer flex-col p-4 transition-shadow hover:shadow-soft">
      <div className="flex items-start justify-between gap-2">
        <div className="flex min-w-0 flex-1 items-start gap-3">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-red-50 text-pdf dark:bg-red-950/30"><FileType2 className="h-5 w-5" /></span>
          <div className="min-w-0 flex-1">
            <p className="line-clamp-2 break-words font-semibold leading-snug text-ink dark:text-slate-100" title={doc.name}>{doc.name}</p>
            <p className="truncate text-sm text-ink-muted">{doc.type}</p>
          </div>
        </div>
        <Menu doc={doc} onDelete={onDelete} onView={() => open(doc)} onAdd={canAdd ? () => setAddOpen(true) : undefined} />
      </div>

      <div className="mt-3 space-y-1 text-xs text-ink-muted">
        <p className="flex items-center gap-1.5 truncate"><Building2 className="h-3.5 w-3.5 shrink-0" /> {doc.authority}</p>
        <p className="flex items-center gap-1.5 truncate"><CalendarDays className="h-3.5 w-3.5 shrink-0" /> {doc.financialYear}</p>
      </div>

      {userTags.length > 0 && (
        <div className="mt-2.5 flex flex-wrap gap-1.5">{userTags.slice(0, 3).map((t) => <Badge key={t}>{t}</Badge>)}</div>
      )}

      <div className="mt-3 flex items-center justify-between border-t border-line pt-3 dark:border-[#1F262C]">
        <div className="flex flex-wrap items-center gap-1.5">
          <ApprovalBadge status={doc.approval} />
          <StatusBadge status={doc.status} />
        </div>
        <div className="flex items-center gap-3 text-xs text-ink-muted">
          {projectCount > 0 && <span className="flex items-center gap-1"><Layers3 className="h-3.5 w-3.5" /> {projectCount} {projectCount === 1 ? 'project' : 'projects'}</span>}
          <span>{doc.expiryDate ? `Expires ${fmtDayMonth(doc.expiryDate)}` : fmtDayMonth(doc.uploadedAt)}</span>
        </div>
      </div>

      <AddToProjectModal doc={doc} open={addOpen} onClose={() => setAddOpen(false)} />
    </motion.div>
  );
}

export function DocumentRow({ doc, onDelete }: { doc: DocumentItem; onDelete?: () => void }) {
  const { open } = useDocumentDrawer();
  const { permissions } = useAuth();
  const canAdd = can(permissions, Permission.ProjectsAssign);
  const [addOpen, setAddOpen] = useState(false);
  const projectCount = useProjectCount(doc.id);

  return (
    <motion.div layout initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
      onClick={() => open(doc)} role="button"
      className="surface flex cursor-pointer items-center gap-4 p-3.5 transition-shadow hover:shadow-soft">
      <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-red-50 text-pdf dark:bg-red-950/30"><FileType2 className="h-5 w-5" /></span>
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink dark:text-slate-100" title={doc.name}>{doc.name}</p>
        <p className="truncate text-sm text-ink-muted">{doc.type} • {doc.authority} • {doc.financialYear}</p>
      </div>
      {projectCount > 0 && <span className="hidden items-center gap-1 text-xs text-ink-muted sm:flex"><Layers3 className="h-3.5 w-3.5" /> {projectCount}</span>}
      <ApprovalBadge status={doc.approval} />
      <StatusBadge status={doc.status} />
      <span className="hidden w-16 text-right text-xs text-ink-muted md:block">{fmtDayMonth(doc.uploadedAt)}</span>
      <Menu doc={doc} onDelete={onDelete} onView={() => open(doc)} onAdd={canAdd ? () => setAddOpen(true) : undefined} />
      <AddToProjectModal doc={doc} open={addOpen} onClose={() => setAddOpen(false)} />
    </motion.div>
  );
}
