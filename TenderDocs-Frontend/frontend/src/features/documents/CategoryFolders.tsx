import { Fragment, useMemo, useState, type ReactNode } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronRight, FileType2, Folder, FolderOpen, Banknote, Wrench, Layers, Building2 } from 'lucide-react';
import { Card, EmptyState } from '@/components/ui';
import { DOCUMENT_CATEGORIES } from '@/types';
import { useDocumentDrawer } from '@/features/documents/DocumentDrawer';
import type { DocumentItem, ProjectItem } from '@/types';
import { cn, fmtDayMonth } from '@/lib/utils';

const GROUPS: { name: string; icon: typeof Banknote; categories: string[] }[] = [
  { name: 'Financial', icon: Banknote, categories: ['Gst', 'Pan', 'Itr', 'Msme', 'Iso', 'BankStatement', 'FinancialDocument'] },
  { name: 'Technical', icon: Wrench, categories: ['TechnicalDocument', 'ExperienceCertificate'] },
  { name: 'Others', icon: Layers, categories: ['Other'] },
];
const labelOf = (v: string) => DOCUMENT_CATEGORIES.find((c) => c.value === v)?.label ?? 'Others';
const normalize = (c?: string) => (c && DOCUMENT_CATEGORIES.some((x) => x.value === c) ? c : 'Other');
const groupOf = (cat: string) => GROUPS.find((g) => g.categories.includes(cat))?.name ?? 'Others';

// The current selection in the tree: the whole scope (root), a group, or a leaf category.
type Sel = { kind: 'root' } | { kind: 'group'; name: string } | { kind: 'cat'; cat: string };
const selKey = (s: Sel) => (s.kind === 'root' ? 'root' : s.kind === 'group' ? `group:${s.name}` : `cat:${s.cat}`);

export function CategoryFolders({ documents, projects }: { documents: DocumentItem[]; projects: ProjectItem[] }) {
  const { open: openDrawer } = useDocumentDrawer();
  const [scopeProject, setScopeProject] = useState<string>('all');

  const scopedDocs = useMemo(() => {
    if (scopeProject === 'all') return documents;
    const p = projects.find((x) => x.id === scopeProject);
    const ids = new Set(p?.documentIds ?? []);
    return documents.filter((d) => ids.has(d.id));
  }, [documents, projects, scopeProject]);

  const byCategory = useMemo(() => {
    const m = new Map<string, DocumentItem[]>();
    for (const d of scopedDocs) {
      const k = normalize(d.category);
      (m.get(k) ?? m.set(k, []).get(k)!).push(d);
    }
    return m;
  }, [scopedDocs]);

  const projectCount = useMemo(() => {
    const m = new Map<string, number>();
    for (const p of projects) for (const id of p.documentIds) m.set(id, (m.get(id) ?? 0) + 1);
    return m;
  }, [projects]);

  const scopeLabel = scopeProject === 'all' ? 'All Documents' : (projects.find((p) => p.id === scopeProject)?.name ?? 'Project');
  const catCount = (c: string) => byCategory.get(c)?.length ?? 0;
  const groupCount = (g: typeof GROUPS[number]) => g.categories.reduce((n, c) => n + catCount(c), 0);

  const [expanded, setExpanded] = useState<Set<string>>(() => new Set(['root', 'group:Financial', 'group:Technical']));
  const isOpen = (k: string) => expanded.has(k);
  const toggle = (k: string) => setExpanded((prev) => { const n = new Set(prev); n.has(k) ? n.delete(k) : n.add(k); return n; });

  const [selected, setSelected] = useState<Sel>({ kind: 'cat', cat: 'Gst' });

  // Files shown on the right for the current selection (root → everything, group → all of its
  // categories, category → just that category).
  const selectedDocs = useMemo(() => {
    if (selected.kind === 'root') return scopedDocs;
    if (selected.kind === 'group') {
      const g = GROUPS.find((x) => x.name === selected.name);
      return g ? g.categories.flatMap((c) => byCategory.get(c) ?? []) : [];
    }
    return byCategory.get(selected.cat) ?? [];
  }, [selected, scopedDocs, byCategory]);

  // Select a node AND make sure its path is expanded so it stays visible in the tree.
  function navigate(sel: Sel) {
    setSelected(sel);
    setExpanded((prev) => {
      const n = new Set(prev).add('root');
      if (sel.kind === 'group') n.add(`group:${sel.name}`);
      if (sel.kind === 'cat') n.add(`group:${groupOf(sel.cat)}`);
      return n;
    });
  }

  const isActive = (sel: Sel) => selKey(selected) === selKey(sel);

  // Breadcrumb path for the current selection.
  const crumbs: { label: string; sel: Sel }[] = [{ label: scopeLabel, sel: { kind: 'root' } }];
  if (selected.kind === 'group') crumbs.push({ label: selected.name, sel: selected });
  else if (selected.kind === 'cat') {
    const g = groupOf(selected.cat);
    crumbs.push({ label: g, sel: { kind: 'group', name: g } });
    crumbs.push({ label: labelOf(selected.cat), sel: selected });
  }

  function TreeButton({ label, depth, count, open, hasChildren, accent, active, onClick, onToggle, glyph }: {
    label: string; depth: number; count: number; open?: boolean; hasChildren?: boolean;
    accent?: boolean; active?: boolean; onClick: () => void; onToggle?: () => void; glyph: ReactNode;
  }) {
    return (
      <div style={{ paddingLeft: 8 + depth * 18 }}
        className={cn('flex w-full items-center rounded-lg pr-3 transition-colors',
          active ? 'bg-brand-50 dark:bg-brand-900/30' : 'hover:bg-slate-50 dark:hover:bg-[#161D23]')}>
        {hasChildren ? (
          <button onClick={(e) => { e.stopPropagation(); onToggle?.(); }} aria-label={open ? 'Collapse' : 'Expand'}
            className="shrink-0 rounded p-0.5 text-ink-faint hover:text-ink dark:hover:text-slate-200">
            <ChevronRight className={cn('h-4 w-4 transition-transform', open && 'rotate-90')} />
          </button>
        ) : <span className="w-5 shrink-0" />}
        <button onClick={onClick} className="flex min-w-0 flex-1 items-center gap-2 py-2 text-left">
          {glyph}
          <span className={cn('flex-1 truncate text-sm', accent ? 'font-semibold text-ink dark:text-slate-100' : active ? 'font-semibold text-brand-700 dark:text-brand-200' : 'font-medium text-ink-soft dark:text-slate-200')}>{label}</span>
          <span className="rounded-full bg-slate-100 px-1.5 py-0.5 text-[11px] font-medium text-ink-muted dark:bg-[#1B232A]">{count}</span>
        </button>
      </div>
    );
  }

  return (
    <Card className="overflow-hidden p-0">
      {/* header + project scope (top-right) */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-4 py-3 dark:border-[#222A31]">
        <div>
          <h3 className="text-sm font-semibold text-ink dark:text-slate-100">Folder structure</h3>
          <p className="text-xs text-ink-muted">Live view — exactly how documents are organized inside project ZIP exports.</p>
        </div>
        <label className="flex items-center gap-2">
          <span className="text-xs font-medium text-ink-muted">Project</span>
          <select value={scopeProject} onChange={(e) => setScopeProject(e.target.value)}
            className="h-9 rounded-lg border border-line bg-white px-2.5 text-sm text-ink outline-none focus:border-brand-400 dark:border-[#222A31] dark:bg-[#12181D] dark:text-slate-100">
            <option value="all">All projects</option>
            {projects.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </label>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-[300px_1fr]">
        {/* LEFT: tree */}
        <div className="border-b border-line p-2 md:border-b-0 md:border-r dark:border-[#222A31]">
          <TreeButton label={scopeLabel} depth={0} count={scopedDocs.length} open={isOpen('root')} hasChildren accent
            active={isActive({ kind: 'root' })} onClick={() => navigate({ kind: 'root' })} onToggle={() => toggle('root')}
            glyph={<Building2 className="h-4 w-4 shrink-0 text-brand-600" />} />
          <AnimatePresence initial={false}>
            {isOpen('root') && (
              <motion.div initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }} className="overflow-hidden">
                {GROUPS.map((g) => {
                  const gKey = `group:${g.name}`;
                  return (
                    <div key={g.name}>
                      <TreeButton label={g.name} depth={1} count={groupCount(g)} open={isOpen(gKey)} hasChildren
                        active={isActive({ kind: 'group', name: g.name })}
                        onClick={() => navigate({ kind: 'group', name: g.name })} onToggle={() => toggle(gKey)}
                        glyph={isOpen(gKey) ? <FolderOpen className="h-4 w-4 shrink-0 text-brand-600" /> : <Folder className="h-4 w-4 shrink-0 text-brand-600" />} />
                      <AnimatePresence initial={false}>
                        {isOpen(gKey) && (
                          <motion.div initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }} className="overflow-hidden">
                            {g.categories.map((c) => {
                              const active = isActive({ kind: 'cat', cat: c });
                              return (
                                <TreeButton key={c} label={labelOf(c)} depth={2} count={catCount(c)} hasChildren={false} active={active}
                                  onClick={() => navigate({ kind: 'cat', cat: c })}
                                  glyph={active ? <FolderOpen className="h-4 w-4 shrink-0 text-amber-500" /> : <Folder className="h-4 w-4 shrink-0 text-amber-500" />} />
                              );
                            })}
                          </motion.div>
                        )}
                      </AnimatePresence>
                    </div>
                  );
                })}
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* RIGHT: files of the selected folder */}
        <div className="min-h-[340px] p-4">
          {/* breadcrumb — each level navigates to that node; the current level is highlighted */}
          <div className="mb-3 flex flex-wrap items-center gap-1 text-xs">
            {crumbs.map((c, i) => {
              const last = i === crumbs.length - 1;
              return (
                <Fragment key={selKey(c.sel)}>
                  {i > 0 && <ChevronRight className="h-3.5 w-3.5 text-ink-faint" />}
                  <button onClick={() => navigate(c.sel)}
                    className={cn('rounded-md px-1.5 py-0.5 font-medium transition-colors',
                      last ? 'bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-200'
                        : 'text-ink-muted hover:bg-slate-100 hover:text-ink dark:hover:bg-[#1B232A] dark:hover:text-slate-100')}>
                    {c.label}
                  </button>
                </Fragment>
              );
            })}
            <span className="ml-1 text-ink-muted">· {selectedDocs.length} {selectedDocs.length === 1 ? 'file' : 'files'}</span>
          </div>

          {selectedDocs.length === 0 ? (
            <EmptyState icon={<FileType2 className="h-6 w-6" />} title="No documents here"
              hint="Link documents to this category in a project's Organize screen to file them here." />
          ) : (
            <div className="space-y-2">
              {selectedDocs.map((d) => {
                const pc = projectCount.get(d.id) ?? 0;
                return (
                  <button key={d.id} onClick={() => openDrawer(d)}
                    className="flex w-full items-center gap-3 rounded-xl border border-line p-3 text-left transition-colors hover:border-brand-300 hover:bg-slate-50/60 dark:border-[#1F262C] dark:hover:bg-[#161D23]">
                    <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-red-50 text-pdf dark:bg-red-950/30"><FileType2 className="h-4 w-4" /></span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium text-ink dark:text-slate-100" title={d.name}>{d.name}</p>
                      <p className="truncate text-xs text-ink-muted">{d.authority} · {fmtDayMonth(d.uploadedAt)}</p>
                    </div>
                    {pc > 0 && <span className="shrink-0 text-xs text-ink-muted">{pc} {pc === 1 ? 'project' : 'projects'}</span>}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}
