import { AnimatePresence, motion } from 'framer-motion';
import {
  ArrowLeft, Folder, Plus, Upload, Download, Search, FileText, GripVertical, Check, CheckCircle2,
  X, FolderArchive, Link2, Info, Pencil, ChevronDown, Trash2, FolderPlus,
} from 'lucide-react';
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Button, Modal, StatusBadge, EmptyState, Skeleton } from '@/components/ui';
import { UploadDialog } from '@/components/layout/UploadDialog';
import { useToast, useDocuments, useOrganizeDetail } from '@/hooks';
import { useAuth } from '@/auth/AuthProvider';
import { can } from '@/lib/access';
import { useDocumentDrawer } from '@/features/documents/DocumentDrawer';
import { apiClients, saveBlob } from '@/services';
import type { ProjectDetailDto, ProjectRequirementDto } from '@/services/api';
import type { DocumentItem } from '@/types';
import { cn, pluralize } from '@/lib/utils';

type ConfirmState = { title: string; message: string; confirmLabel: string; onConfirm: () => void };

/* ------------------------------------------------------------------ *
 * Inline text editor — shared by category and row renames / adds.    *
 * ------------------------------------------------------------------ */
function InlineEdit({ value, placeholder, onSubmit, onCancel, className }: {
  value?: string; placeholder?: string; onSubmit: (v: string) => void; onCancel: () => void; className?: string;
}) {
  return (
    <input
      autoFocus
      defaultValue={value}
      placeholder={placeholder}
      onBlur={(e) => onSubmit(e.target.value)}
      onKeyDown={(e) => {
        if (e.key === 'Enter') onSubmit((e.target as HTMLInputElement).value);
        if (e.key === 'Escape') onCancel();
      }}
      className={cn(
        'w-full rounded-md border border-brand-400 bg-white px-1.5 py-0.5 text-sm text-ink outline-none focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:text-slate-100',
        className,
      )}
    />
  );
}

/* ------------------------------------------------------------------ *
 * Reusable collapsible category section (used for every category).  *
 * Renders the dropdown header + Add Row / rename / delete controls,  *
 * and shows its rows (passed as children) only when expanded.        *
 * ------------------------------------------------------------------ */
function CategorySection({
  name, rowCount, filledCount, expanded, onToggle, editing, canEdit,
  onStartRename, onRename, onCancelRename, onAddRow, onDelete, children,
}: {
  name: string; rowCount: number; filledCount: number; expanded: boolean; onToggle: () => void;
  editing: boolean; canEdit: boolean; onStartRename: () => void; onRename: (v: string) => void; onCancelRename: () => void;
  onAddRow: () => void; onDelete: () => void; children: ReactNode;
}) {
  return (
    <div className="border-b border-line last:border-0 dark:border-[#1F262C]">
      {/* dropdown header */}
      <div className="group/cat flex items-center gap-2 bg-slate-50/80 px-3 py-2.5 dark:bg-[#12181D]">
        <button
          onClick={onToggle}
          className="flex flex-1 items-center gap-2 text-left"
          aria-expanded={expanded}
          aria-label={expanded ? `Collapse ${name}` : `Expand ${name}`}
        >
          <motion.span animate={{ rotate: expanded ? 0 : -90 }} transition={{ duration: 0.18 }} className="text-ink-muted">
            <ChevronDown className="h-4 w-4" />
          </motion.span>
          <Folder className="h-4 w-4 text-brand-600" />
          {editing ? (
            <div onClick={(e) => e.stopPropagation()} className="min-w-0 flex-1">
              <InlineEdit value={name} onSubmit={onRename} onCancel={onCancelRename} className="font-semibold" />
            </div>
          ) : (
            <span className="truncate text-sm font-semibold uppercase tracking-wide text-ink-soft dark:text-slate-300" title={name}>
              {name}
            </span>
          )}
          <span className="rounded-full bg-white px-1.5 py-0.5 text-[11px] font-medium text-ink-muted ring-1 ring-line dark:bg-[#1B232A] dark:ring-[#222A31]">
            {filledCount}/{rowCount}
          </span>
          <span className="hidden text-[11px] text-ink-faint sm:inline">→ {name}/ in export</span>
        </button>
        {canEdit && !editing && (
          <div className="flex items-center gap-0.5 opacity-0 transition-opacity group-hover/cat:opacity-100">
            <button onClick={onStartRename} className="rounded p-1 text-ink-faint hover:text-brand-600" aria-label="Rename category"><Pencil className="h-3.5 w-3.5" /></button>
            <button onClick={onDelete} className="rounded p-1 text-ink-faint hover:text-danger-text" aria-label="Delete category"><Trash2 className="h-3.5 w-3.5" /></button>
          </div>
        )}
        {canEdit && (
          <button
            onClick={onAddRow}
            className="inline-flex items-center gap-1 rounded-lg bg-white px-2 py-1 text-xs font-medium text-brand-700 ring-1 ring-line transition-colors hover:bg-brand-50 dark:bg-[#1B232A] dark:text-brand-200 dark:ring-[#222A31]"
          >
            <Plus className="h-3.5 w-3.5" /> Add Row
          </button>
        )}
      </div>

      {/* rows */}
      <AnimatePresence initial={false}>
        {expanded && (
          <motion.div
            initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2 }} className="overflow-hidden"
          >
            {children}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

export function OrganizeWorkspace({ projectId, onBack }: { projectId: string; onBack?: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const { role } = useAuth();
  const canEdit = !!role && can(role, 'organizeEdit');   // approver only; viewers are read-only
  const canManage = !!role && can(role, 'manageProject'); // add documents to the project
  const { open: openDrawer } = useDocumentDrawer();
  const { data: detail, isLoading } = useOrganizeDetail(projectId);
  const { data: allDocs = [] } = useDocuments();

  const [busyDoc, setBusyDoc] = useState<string | null>(null);
  const [dragDoc, setDragDoc] = useState<string | null>(null);
  const [overRow, setOverRow] = useState<string | null>(null);
  const [justLinked, setJustLinked] = useState<string | null>(null);
  const [query, setQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [upload, setUpload] = useState(false);
  const [addOpen, setAddOpen] = useState(false);
  const [zipOpen, setZipOpen] = useState(false);
  const [zipPhase, setZipPhase] = useState<'building' | 'done'>('building');

  // structure editing state
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  const [editingCat, setEditingCat] = useState<string | null>(null);
  const [editingRow, setEditingRow] = useState<string | null>(null);
  const [addingRowFor, setAddingRowFor] = useState<string | null>(null);
  const [addingCategory, setAddingCategory] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmState | null>(null);

  const categories = useMemo(() => detail?.categories ?? [], [detail]);
  const allRows = useMemo(() => categories.flatMap((c) => c.requirements), [categories]);
  const rowById = useMemo(() => new Map(allRows.map((r) => [r.id, r])), [allRows]);

  // docId -> requirementId (null = in project but unmapped)
  const assignmentByDoc = useMemo(() => {
    const m = new Map<string, string | null>();
    (detail?.assignments ?? []).forEach((a) => m.set(a.documentId, a.requirementId));
    return m;
  }, [detail]);

  const projectDocIds = useMemo(() => new Set((detail?.documents ?? []).map((d) => d.id)), [detail]);
  const projectDocs = useMemo(() => allDocs.filter((d) => projectDocIds.has(d.id)), [allDocs, projectDocIds]);
  const available = useMemo(() => allDocs.filter((d) => !projectDocIds.has(d.id)), [allDocs, projectDocIds]);

  const linkedSet = useMemo(() => {
    const s = new Set<string>();
    projectDocs.forEach((d) => { if (assignmentByDoc.get(d.id)) s.add(d.id); });
    return s;
  }, [projectDocs, assignmentByDoc]);

  const docsForRow = useCallback(
    (reqId: string) => projectDocs.filter((d) => assignmentByDoc.get(d.id) === reqId),
    [projectDocs, assignmentByDoc],
  );
  const rowName = useCallback((reqId: string) => rowById.get(reqId)?.name ?? 'requirement', [rowById]);
  const categoryOfRow = useCallback(
    (reqId?: string | null) => (reqId ? categories.find((c) => c.requirements.some((r) => r.id === reqId)) : undefined),
    [categories],
  );

  // First open of a project that has no structure yet → seed the defaults once.
  const ensuredRef = useRef(false);
  useEffect(() => {
    if (!detail || ensuredRef.current) return;
    if (detail.categories.length === 0) {
      ensuredRef.current = true;
      apiClients.OrganizeApi.ensureRequirements(projectId)
        .then((dto) => qc.setQueryData(['organize', projectId], dto))
        .catch(() => { ensuredRef.current = false; });
    }
  }, [detail, projectId, qc]);

  async function refresh() {
    await Promise.all([
      qc.invalidateQueries({ queryKey: ['organize', projectId] }),
      qc.invalidateQueries({ queryKey: ['documents'] }),
      qc.invalidateQueries({ queryKey: ['project', projectId] }),
      qc.invalidateQueries({ queryKey: ['projects'] }),
    ]);
  }

  // Structure mutations all return the refreshed project → push it straight into the cache.
  async function applyStructure(p: Promise<ProjectDetailDto>, ok?: string) {
    try {
      const dto = await p;
      qc.setQueryData(['organize', projectId], dto);
      if (ok) toast.push({ title: ok, tone: 'success' });
    } catch (err) {
      toast.push({ title: (err as Error)?.message || 'Something went wrong', tone: 'danger' });
    }
  }

  async function assign(reqId: string, docId: string | null) {
    if (!docId) return;
    if (assignmentByDoc.get(docId) === reqId) return;
    setBusyDoc(docId);
    try {
      await apiClients.OrganizeApi.assign(projectId, docId, reqId);
      await refresh();
      setJustLinked(`${reqId}::${docId}`);
      setTimeout(() => setJustLinked(null), 900);
      toast.push({ title: `Linked to ${rowName(reqId)}`, tone: 'success' });
    } catch (err) {
      toast.push({ title: (err as Error)?.message || 'Could not link document', tone: 'danger' });
    } finally {
      setBusyDoc(null);
    }
  }

  async function unmap(docId: string) {
    setBusyDoc(docId);
    try {
      await apiClients.OrganizeApi.assign(projectId, docId, undefined);
      await refresh();
    } catch (err) {
      toast.push({ title: (err as Error)?.message || 'Could not unlink', tone: 'danger' });
    } finally {
      setBusyDoc(null);
    }
  }

  async function addToProject(docId: string) {
    setBusyDoc(docId);
    try {
      await apiClients.ProjectsApi.addDocument(projectId, docId);
      await refresh();
      toast.push({ title: 'Added to project', tone: 'success' });
    } catch {
      toast.push({ title: 'Could not add document', tone: 'danger' });
    } finally {
      setBusyDoc(null);
    }
  }

  // ---- category / row CRUD ----
  function createCategory(name: string) {
    setAddingCategory(false);
    if (!name.trim()) return;
    applyStructure(apiClients.OrganizeApi.createCategory(projectId, name.trim()), 'Category added');
  }
  function renameCategory(id: string, name: string) {
    setEditingCat(null);
    const current = categories.find((c) => c.id === id)?.name;
    if (!name.trim() || name.trim() === current) return;
    applyStructure(apiClients.OrganizeApi.renameCategory(projectId, id, name.trim()));
  }
  function deleteCategory(id: string) {
    const cat = categories.find((c) => c.id === id);
    const count = cat?.requirements.length ?? 0;
    setConfirm({
      title: `Delete "${cat?.name}"?`,
      message: count > 0
        ? `This removes the category and its ${pluralize(count, 'sub-category')}. Any linked documents stay in the project but become unmapped.`
        : 'This removes the category.',
      confirmLabel: 'Delete category',
      onConfirm: () => { setConfirm(null); applyStructure(apiClients.OrganizeApi.deleteCategory(projectId, id), 'Category deleted'); },
    });
  }
  function createRow(categoryId: string, name: string) {
    setAddingRowFor(null);
    if (!name.trim()) return;
    applyStructure(apiClients.OrganizeApi.createRequirement(projectId, categoryId, name.trim()), 'Sub-category added');
  }
  function renameRow(id: string, name: string) {
    setEditingRow(null);
    const current = rowById.get(id)?.name;
    if (!name.trim() || name.trim() === current) return;
    applyStructure(apiClients.OrganizeApi.renameRequirement(projectId, id, name.trim()));
  }
  function deleteRow(id: string) {
    const count = docsForRow(id).length;
    setConfirm({
      title: `Delete "${rowName(id)}"?`,
      message: count > 0
        ? `${pluralize(count, 'document')} mapped here will stay in the project but become unmapped.`
        : 'This removes the sub-category row.',
      confirmLabel: 'Delete row',
      onConfirm: () => { setConfirm(null); applyStructure(apiClients.OrganizeApi.deleteRequirement(projectId, id), 'Sub-category deleted'); },
    });
  }

  function downloadZip() {
    setZipPhase('building'); setZipOpen(true);
    apiClients.ProjectsApi.downloadZip(projectId)
      .then((blob) => { saveBlob(blob, `${detail?.name ?? 'project'}.zip`); setZipPhase('done'); })
      .catch(() => { setZipPhase('done'); toast.push({ title: 'Could not generate ZIP', tone: 'danger' }); });
  }

  /* ---------- filter predicate (shared by the available list AND the connector lines) ---------- */
  const matchesFilter = useCallback((d: DocumentItem) => {
    const reqId = assignmentByDoc.get(d.id) ?? null;
    if (typeFilter === 'unmapped' && reqId) return false;
    if (typeFilter !== 'all' && typeFilter !== 'unmapped') {
      const cat = categoryOfRow(reqId);
      if (cat?.id !== typeFilter) return false;
    }
    const q = query.trim().toLowerCase();
    if (!q) return true;
    const rn = reqId ? rowName(reqId).toLowerCase() : '';
    return d.name.toLowerCase().includes(q) || d.authority.toLowerCase().includes(q) || rn.includes(q);
  }, [typeFilter, query, assignmentByDoc, categoryOfRow, rowName]);

  const visibleDocs = useMemo(() => projectDocs.filter(matchesFilter), [projectDocs, matchesFilter]);

  /* ---------- connector geometry (row port <-> linked doc port) ---------- */
  const containerRef = useRef<HTMLDivElement>(null);
  const rowAnchors = useRef<Record<string, HTMLSpanElement | null>>({});
  const docAnchors = useRef<Record<string, HTMLSpanElement | null>>({});
  const [paths, setPaths] = useState<{ key: string; d: string; x1: number; y1: number; x2: number; y2: number }[]>([]);
  const [overlay, setOverlay] = useState({ w: 0, h: 0 });

  const compute = useCallback(() => {
    const cEl = containerRef.current;
    if (!cEl) return;
    const c = cEl.getBoundingClientRect();
    const next: typeof paths = [];
    for (const row of allRows) {
      const ra = rowAnchors.current[row.id];
      if (!ra) continue;                       // row hidden (collapsed category) → no line
      const r = ra.getBoundingClientRect();
      const x1 = r.left - c.left + r.width / 2;
      const y1 = r.top - c.top + r.height / 2;
      for (const d of docsForRow(row.id)) {
        if (!matchesFilter(d)) continue;
        const da = docAnchors.current[d.id];
        if (!da) continue;
        const dd = da.getBoundingClientRect();
        const x2 = dd.left - c.left + dd.width / 2;
        const y2 = dd.top - c.top + dd.height / 2;
        const dx = Math.max(48, Math.abs(x2 - x1) * 0.45);
        next.push({ key: `${row.id}::${d.id}`, d: `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`, x1, y1, x2, y2 });
      }
    }
    setPaths(next);
    setOverlay({ w: cEl.offsetWidth, h: cEl.offsetHeight });
  }, [allRows, docsForRow, matchesFilter]);

  useLayoutEffect(() => { compute(); }, [compute, collapsed, detail]);
  useEffect(() => {
    const t1 = setTimeout(compute, 90);
    const t2 = setTimeout(compute, 380);
    return () => { clearTimeout(t1); clearTimeout(t2); };
  }, [compute, collapsed]);
  useEffect(() => {
    const cEl = containerRef.current;
    const ro = new ResizeObserver(() => compute());
    if (cEl) ro.observe(cEl);
    const onWin = () => compute();
    window.addEventListener('resize', onWin);
    window.addEventListener('scroll', onWin, true);
    return () => { ro.disconnect(); window.removeEventListener('resize', onWin); window.removeEventListener('scroll', onWin, true); };
  }, [compute]);

  /* ---------- filter chips: All / Unmapped / one per category ---------- */
  const filters = useMemo(() => ['all', 'unmapped', ...categories.map((c) => c.id)], [categories]);
  const filterLabel = (f: string) =>
    f === 'all' ? 'All' : f === 'unmapped' ? 'Unmapped' : categories.find((c) => c.id === f)?.name ?? 'Category';

  if (isLoading || !detail) {
    return <div className="space-y-4"><Skeleton className="h-28" /><Skeleton className="h-72" /></div>;
  }

  const totalRows = allRows.length;
  const filledRows = allRows.filter((r) => docsForRow(r.id).length > 0).length;
  const completion = totalRows ? Math.round((filledRows / totalRows) * 100) : 0;

  function renderRow(row: ProjectRequirementDto) {
    const docs = docsForRow(row.id);
    const linked = docs.length > 0;
    const isOver = overRow === row.id;
    return (
      <div
        key={row.id}
        onDragOver={canEdit ? (e) => { if (!dragDoc) return; e.preventDefault(); e.dataTransfer.dropEffect = 'link'; if (overRow !== row.id) setOverRow(row.id); } : undefined}
        onDragLeave={canEdit ? () => setOverRow((c) => (c === row.id ? null : c)) : undefined}
        onDrop={canEdit ? (e) => { e.preventDefault(); const id = e.dataTransfer.getData('text/docId') || dragDoc; assign(row.id, id); setOverRow(null); setDragDoc(null); } : undefined}
        className={cn('grid grid-cols-[1.1fr_1.5fr_auto] items-center gap-3 border-b border-line py-3 pl-9 pr-5 transition-colors last:border-0 dark:border-[#1F262C]', isOver && 'bg-brand-50/70 dark:bg-brand-900/20')}
      >
        {/* name (renamable) */}
        <div className="group/name flex min-w-0 items-center gap-1.5">
          {editingRow === row.id ? (
            <InlineEdit value={row.name} onSubmit={(v) => renameRow(row.id, v)} onCancel={() => setEditingRow(null)} />
          ) : (
            <>
              <span className="truncate text-sm font-medium text-ink dark:text-slate-100" title={row.name}>{row.name}</span>
              {canEdit && <button onClick={() => setEditingRow(row.id)} className="shrink-0 rounded p-0.5 text-ink-faint opacity-0 transition-opacity hover:text-brand-600 group-hover/name:opacity-100" aria-label="Rename"><Pencil className="h-3 w-3" /></button>}
              {canEdit && <button onClick={() => deleteRow(row.id)} className="shrink-0 rounded p-0.5 text-ink-faint opacity-0 transition-opacity hover:text-danger-text group-hover/name:opacity-100" aria-label="Delete row"><Trash2 className="h-3 w-3" /></button>}
            </>
          )}
        </div>
        {/* documents drop area */}
        <div className={cn('relative flex min-h-[44px] flex-wrap items-center gap-1.5 rounded-xl border border-dashed px-2.5 py-1.5 transition-all',
          isOver ? 'border-brand-500 bg-brand-50/80 dark:bg-brand-900/20' : docs.length ? 'border-transparent' : 'border-line dark:border-[#222A31]')}>
          {docs.map((d) => {
            const glow = justLinked === `${row.id}::${d.id}`;
            return (
              <motion.span key={d.id} layout initial={{ scale: 0.8, opacity: 0 }}
                animate={glow ? { scale: [1, 1.06, 1], opacity: 1 } : { scale: 1, opacity: 1 }} transition={{ duration: glow ? 0.5 : 0.2 }}
                className="inline-flex items-center gap-1.5 rounded-lg bg-valid-bg px-2 py-1 text-xs font-medium text-valid-text ring-1 ring-inset ring-valid-ring">
                <Check className="h-3 w-3" />
                <span className="max-w-[140px] truncate" title={d.name}>{d.name}</span>
                {canEdit && <button onClick={() => unmap(d.id)} disabled={busyDoc === d.id} className="rounded-full p-0.5 text-valid-text/70 hover:bg-valid-text/10 hover:text-valid-text" aria-label="Unlink"><X className="h-3 w-3" /></button>}
              </motion.span>
            );
          })}
          {docs.length === 0 && (
            <span className="flex items-center gap-1.5 px-1 text-xs text-ink-faint"><Link2 className="h-3.5 w-3.5" />{!canEdit ? 'No documents' : isOver ? 'Drop to link' : 'Drag a document here'}</span>
          )}
          <span ref={(el) => { rowAnchors.current[row.id] = el; }}
            className={cn('absolute -right-[7px] top-1/2 h-3 w-3 -translate-y-1/2 rounded-full ring-2 ring-white transition-colors dark:ring-[#12181D]', linked ? 'bg-brand-600' : 'bg-slate-300 dark:bg-[#2A333B]')} />
        </div>
        {/* status */}
        <div className="flex items-center justify-end">
          {linked ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-valid-bg px-2 py-1 text-xs font-medium text-valid-text ring-1 ring-inset ring-valid-ring"><CheckCircle2 className="h-3.5 w-3.5" /> Linked</span>
          ) : (
            <span className="inline-flex items-center rounded-full bg-slate-100 px-2 py-1 text-xs font-medium text-ink-muted dark:bg-[#1B232A]">Pending</span>
          )}
        </div>
      </div>
    );
  }

  return (
    <motion.div initial={{ opacity: 0, y: 18 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.4 }}>
      {onBack && (
        <button onClick={onBack} className="mb-4 inline-flex items-center gap-1.5 text-sm font-medium text-ink-muted hover:text-brand-600">
          <ArrowLeft className="h-4 w-4" /> Back to project
        </button>
      )}

      {/* header */}
      <div className="surface overflow-hidden bg-gradient-to-br from-hero-from to-hero-to dark:from-[#12181D] dark:to-[#12181D]">
        <div className="flex flex-col gap-4 p-6 md:flex-row md:items-center md:justify-between">
          <div className="flex items-center gap-4">
            <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-white/70 text-brand-600 shadow-card dark:bg-[#1B232A]"><Folder className="h-7 w-7" /></span>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-ink dark:text-slate-100">Organize · {detail.name}</h1>
              <p className="mt-0.5 text-sm text-ink-muted">{pluralize(linkedSet.size, 'document')} linked<span className="mx-1.5 text-ink-faint">•</span>{pluralize(projectDocs.length, 'document')} in project</p>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {canManage && <Button variant="secondary" size="sm" onClick={() => setAddOpen(true)}><Plus className="h-4 w-4" /> Add Existing</Button>}
            {canManage && <Button variant="secondary" size="sm" onClick={() => setUpload(true)}><Upload className="h-4 w-4" /> Upload New</Button>}
            <Button size="sm" onClick={downloadZip}><Download className="h-4 w-4" /> Download ZIP</Button>
          </div>
        </div>
        <div className="border-t border-line/70 bg-white/50 px-6 py-4 dark:border-[#222A31] dark:bg-[#12181D]">
          <div className="flex items-center justify-between text-sm">
            <span className="font-medium text-ink-soft dark:text-slate-300">Requirement coverage</span>
            <span className="font-semibold text-brand-700 dark:text-brand-300">{completion}%</span>
          </div>
          <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-200/80 dark:bg-[#222A31]">
            <motion.div className="h-full rounded-full bg-gradient-to-r from-brand-500 to-brand-700" initial={false} animate={{ width: `${completion}%` }} transition={{ type: 'spring', stiffness: 120, damping: 20 }} />
          </div>
        </div>
      </div>

      {/* explanation */}
      <div className="mt-5 flex items-start gap-3 rounded-2xl border border-brand-200 bg-brand-50/60 p-4 text-sm dark:border-brand-900/40 dark:bg-brand-900/10">
        <Info className="mt-0.5 h-5 w-5 shrink-0 text-brand-600" />
        <div className="text-ink-soft dark:text-slate-300">
          <p>Each <span className="font-medium">category</span> (e.g. Financial, Technical) holds sub-categories you can rename, add to, or remove. Drag a document from <span className="font-medium">Available Documents</span> onto a sub-category to link it — each document maps to one at a time.</p>
          <p className="mt-1 text-ink-muted">Categories become folders in the exported ZIP and sub-category names tag each file, so any rename here is reflected in the download. Unlinked documents land in <span className="font-medium text-ink dark:text-slate-200">Others/</span>.</p>
        </div>
      </div>

      {/* workspace: requirements (left) + available (right) + connectors */}
      <div ref={containerRef} className="relative mt-6">
        <svg className="pointer-events-none absolute inset-0 z-20" width={overlay.w} height={overlay.h} style={{ overflow: 'visible' }}>
          <defs>
            <linearGradient id="connector" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor="#16A34A" /><stop offset="100%" stopColor="#0F766E" />
            </linearGradient>
            <style>{`@keyframes td-flow { to { stroke-dashoffset: -28; } }`}</style>
          </defs>
          <AnimatePresence>
            {paths.map((p) => (
              <motion.g key={p.key} initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.25 }}>
                <path d={p.d} fill="none" stroke="#16A34A" strokeOpacity={0.18} strokeWidth={8} strokeLinecap="round" />
                <motion.path d={p.d} fill="none" stroke="url(#connector)" strokeWidth={2.5} strokeLinecap="round"
                  initial={{ pathLength: 0 }} animate={{ pathLength: 1 }} transition={{ duration: 0.45, ease: 'easeInOut' }} />
                <path d={p.d} fill="none" stroke="url(#connector)" strokeWidth={2.5} strokeLinecap="round"
                  strokeDasharray="7 7" style={{ animation: 'td-flow 0.8s linear infinite' }} />
                <circle r={3.5} fill="#0F766E">
                  <animateMotion dur="1.6s" repeatCount="indefinite" path={p.d} />
                </circle>
                <circle cx={p.x1} cy={p.y1} r={4.5} fill="#16A34A" />
                <circle cx={p.x2} cy={p.y2} r={4.5} fill="#0F766E" />
              </motion.g>
            ))}
          </AnimatePresence>
        </svg>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_340px]">
          {/* requirements table */}
          <div className="surface relative z-10 overflow-hidden">
            <div className="flex items-center justify-between border-b border-line px-5 py-3.5 dark:border-[#222A31]">
              <div>
                <h2 className="text-sm font-semibold text-ink dark:text-slate-100">Requirements</h2>
                <p className="text-xs text-ink-muted">Organize documents into categories and sub-categories</p>
              </div>
              {canEdit && <Button variant="soft" size="sm" onClick={() => setAddingCategory(true)}><FolderPlus className="h-4 w-4" /> Add Category</Button>}
            </div>
            <div className="grid grid-cols-[1.1fr_1.5fr_auto] items-center gap-3 border-b border-line bg-slate-50/60 px-5 py-2 text-[11px] font-semibold uppercase tracking-wide text-ink-faint dark:border-[#222A31] dark:bg-[#12181D]">
              <span>Sub-category</span><span>Documents</span><span className="text-right">Status</span>
            </div>

            <div>
              {categories.map((cat) => {
                const expanded = !collapsed.has(cat.id);
                const filled = cat.requirements.filter((r) => docsForRow(r.id).length > 0).length;
                return (
                  <CategorySection
                    key={cat.id}
                    name={cat.name}
                    rowCount={cat.requirements.length}
                    filledCount={filled}
                    expanded={expanded}
                    canEdit={canEdit}
                    onToggle={() => setCollapsed((s) => { const n = new Set(s); n.has(cat.id) ? n.delete(cat.id) : n.add(cat.id); return n; })}
                    editing={editingCat === cat.id}
                    onStartRename={() => setEditingCat(cat.id)}
                    onRename={(v) => renameCategory(cat.id, v)}
                    onCancelRename={() => setEditingCat(null)}
                    onAddRow={() => { setCollapsed((s) => { const n = new Set(s); n.delete(cat.id); return n; }); setAddingRowFor(cat.id); }}
                    onDelete={() => deleteCategory(cat.id)}
                  >
                    {cat.requirements.map(renderRow)}
                    {addingRowFor === cat.id && (
                      <div className="border-b border-line py-2.5 pl-9 pr-5 last:border-0 dark:border-[#1F262C]">
                        <InlineEdit placeholder="New sub-category name…" onSubmit={(v) => createRow(cat.id, v)} onCancel={() => setAddingRowFor(null)} />
                      </div>
                    )}
                    {cat.requirements.length === 0 && addingRowFor !== cat.id && (
                      <div className="py-3 pl-9 pr-5 text-xs text-ink-faint">No sub-categories yet — use <span className="font-medium">Add Row</span> to create one.</div>
                    )}
                  </CategorySection>
                );
              })}

              {addingCategory && (
                <div className="flex items-center gap-2 bg-slate-50/80 px-3 py-2.5 dark:bg-[#12181D]">
                  <FolderPlus className="h-4 w-4 text-brand-600" />
                  <div className="max-w-xs flex-1"><InlineEdit placeholder="New category name…" onSubmit={createCategory} onCancel={() => setAddingCategory(false)} /></div>
                </div>
              )}

              {categories.length === 0 && !addingCategory && (
                <div className="px-5 py-10">
                  <EmptyState icon={<Folder className="h-5 w-5" />} title="No categories yet" hint="Add a category to start organizing this project's documents."
                    action={canEdit ? <Button variant="soft" size="sm" onClick={() => setAddingCategory(true)}><FolderPlus className="h-4 w-4" /> Add Category</Button> : undefined} />
                </div>
              )}
            </div>
          </div>

          {/* available documents */}
          <div className="relative z-10">
            <div className="surface p-4">
              <h2 className="mb-3 text-sm font-semibold text-ink dark:text-slate-100">Available Documents</h2>
              <div className="relative mb-2.5">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-faint" />
                <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search documents…"
                  className="h-9 w-full rounded-xl border border-line bg-white pl-9 pr-3 text-sm text-ink outline-none placeholder:text-ink-faint focus:border-brand-400 focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100" />
              </div>
              <div className="mb-3 flex flex-wrap gap-1.5">
                {filters.map((f) => (
                  <button key={f} onClick={() => setTypeFilter(f)}
                    className={cn('rounded-full px-2.5 py-1 text-xs font-medium transition-colors',
                      typeFilter === f ? 'bg-brand-600 text-white'
                        : f === 'unmapped' ? 'bg-warn-bg text-warn-text hover:bg-warn-bg/80'
                        : 'bg-slate-100 text-ink-soft hover:bg-slate-200 dark:bg-[#1B232A] dark:text-slate-300')}>
                    {filterLabel(f)}
                  </button>
                ))}
              </div>
              <div className="flex flex-col gap-2">
                {visibleDocs.length === 0 && <EmptyState icon={<FileText className="h-5 w-5" />} title="No documents" hint="Add documents to this project first." />}
                {visibleDocs.map((d) => {
                  const isLinked = linkedSet.has(d.id);
                  const cat = categoryOfRow(assignmentByDoc.get(d.id));
                  return (
                    <div key={d.id} draggable={canEdit}
                      onDragStart={canEdit ? (e) => { e.dataTransfer.setData('text/docId', d.id); e.dataTransfer.effectAllowed = 'link'; setDragDoc(d.id); } : undefined}
                      onClick={() => openDrawer(d)}
                      onDragEnd={canEdit ? () => { setDragDoc(null); setOverRow(null); } : undefined}
                      className={cn('group relative flex items-center gap-2.5 rounded-xl border bg-white px-3 py-2.5 shadow-card transition-all dark:bg-[#12181D]',
                        canEdit ? 'cursor-grab active:cursor-grabbing' : 'cursor-pointer',
                        isLinked ? 'border-valid-ring ring-1 ring-valid-ring' : 'border-line hover:border-brand-300 hover:shadow-soft dark:border-[#222A31]', dragDoc === d.id && 'opacity-50')}>
                      <span ref={(el) => { docAnchors.current[d.id] = el; }}
                        className={cn('absolute -left-[7px] top-1/2 h-3 w-3 -translate-y-1/2 rounded-full ring-2 ring-white transition-colors dark:ring-[#12181D]', isLinked ? 'bg-valid-text' : 'bg-slate-300 opacity-0 group-hover:opacity-100 dark:bg-[#2A333B]')} />
                      <GripVertical className="h-4 w-4 shrink-0 text-ink-faint" />
                      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-red-50 text-pdf dark:bg-red-950/30"><FileText className="h-4 w-4" /></span>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium text-ink dark:text-slate-100" title={d.name}>{d.name}</p>
                        <p className="truncate text-xs text-ink-muted">{isLinked ? `${cat?.name ? `${cat.name} · ` : ''}${rowName(assignmentByDoc.get(d.id)!)}` : 'Unlinked'}</p>
                      </div>
                      {isLinked ? (
                        <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-valid-text text-white"><Check className="h-3 w-3" /></span>
                      ) : (
                        <StatusBadge status={d.status} />
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Add Existing modal */}
      <Modal open={addOpen} onClose={() => setAddOpen(false)} title="Add existing documents" width="max-w-lg">
        <div className="max-h-80 space-y-2 overflow-y-auto">
          {available.length === 0 ? <p className="py-8 text-center text-sm text-ink-muted">Every document is already in this project.</p> :
            available.map((d) => (
              <div key={d.id} className="flex items-center gap-3 rounded-xl border border-line p-3 dark:border-[#1F262C]">
                <FileText className="h-5 w-5 text-pdf" />
                <div className="min-w-0 flex-1"><p className="truncate text-sm font-medium text-ink dark:text-slate-100">{d.name}</p><p className="truncate text-xs text-ink-muted">{d.type}</p></div>
                <Button size="sm" variant="soft" loading={busyDoc === d.id} onClick={() => addToProject(d.id)}><Plus className="h-4 w-4" /> Add</Button>
              </div>
            ))}
        </div>
      </Modal>

      {/* Delete confirm modal */}
      <Modal open={!!confirm} onClose={() => setConfirm(null)} title={confirm?.title} width="max-w-md">
        <p className="text-sm text-ink-soft dark:text-slate-300">{confirm?.message}</p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" size="sm" onClick={() => setConfirm(null)}>Cancel</Button>
          <Button variant="danger" size="sm" onClick={() => confirm?.onConfirm()}><Trash2 className="h-4 w-4" /> {confirm?.confirmLabel}</Button>
        </div>
      </Modal>

      {/* ZIP modal */}
      <Modal open={zipOpen} onClose={() => setZipOpen(false)} title="Generate package" width="max-w-md">
        {zipPhase === 'building' ? (
          <div className="flex flex-col items-center py-8">
            <motion.div animate={{ rotate: 360 }} transition={{ repeat: Infinity, duration: 1.2, ease: 'linear' }} className="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-900/40"><FolderArchive className="h-7 w-7" /></motion.div>
            <p className="text-sm font-medium text-ink dark:text-slate-100">Building {detail.name} package…</p>
            <p className="mt-1 text-xs text-ink-muted">Sorting into your categories + Others</p>
          </div>
        ) : (
          <div className="py-2">
            <div className="mb-4 flex items-center gap-3">
              <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-valid-bg text-valid-text"><CheckCircle2 className="h-5 w-5" /></span>
              <div><p className="text-sm font-semibold text-ink dark:text-slate-100">{detail.name}.zip is ready</p><p className="text-xs text-ink-muted">One folder per category · Others/</p></div>
            </div>
            <div className="mt-5 flex justify-end"><Button onClick={() => setZipOpen(false)}>Done</Button></div>
          </div>
        )}
      </Modal>

      <UploadDialog open={upload} onClose={() => setUpload(false)} projectId={projectId} />
    </motion.div>
  );
}
