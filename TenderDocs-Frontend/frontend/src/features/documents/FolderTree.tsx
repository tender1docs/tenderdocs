import { useMemo, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import {
  ChevronRight, Folder, FolderOpen, FileType2, Home,
} from 'lucide-react';
import { StatusBadge, EmptyState } from '@/components/ui';
import { cn, fmtDayMonth } from '@/lib/utils';
import type { DocumentItem, FolderNode } from '@/types';

function TreeNode({ node, folders, depth, currentId, onSelect, expanded, toggle }: {
  node: FolderNode; folders: FolderNode[]; depth: number; currentId: string;
  onSelect: (id: string) => void; expanded: Set<string>; toggle: (id: string) => void;
}) {
  const children = folders.filter((f) => f.parentId === node.id);
  const hasChildren = children.length > 0;
  const isOpen = expanded.has(node.id);
  const isActive = currentId === node.id;
  return (
    <div>
      <div
        className={cn('flex w-full items-center gap-1.5 rounded-lg py-1.5 pr-2 text-sm transition-colors',
          isActive ? 'bg-brand-50 font-medium text-brand-700 dark:bg-brand-900/40 dark:text-brand-200'
                   : 'text-ink-soft hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-[#161D23]')}
        style={{ paddingLeft: depth * 14 + 8 }}>
        {/* Chevron toggles expansion only (independent of selection). */}
        {hasChildren ? (
          <button
            type="button"
            onClick={(e) => { e.stopPropagation(); toggle(node.id); }}
            className="flex h-5 w-5 shrink-0 items-center justify-center rounded hover:bg-slate-200/70 dark:hover:bg-[#222A31]"
            aria-label={isOpen ? 'Collapse' : 'Expand'}
          >
            <ChevronRight className={cn('h-3.5 w-3.5 transition-transform', isOpen && 'rotate-90')} />
          </button>
        ) : <span className="w-5 shrink-0" />}
        {/* Label selects (and opens) the folder. */}
        <button
          type="button"
          onClick={() => onSelect(node.id)}
          className="flex min-w-0 flex-1 items-center gap-1.5 text-left"
        >
          {isActive || isOpen ? <FolderOpen className="h-4 w-4 shrink-0 text-brand-600" /> : <Folder className="h-4 w-4 shrink-0 text-ink-muted" />}
          <span className="truncate">{node.name}</span>
        </button>
      </div>
      <AnimatePresence initial={false}>
        {isOpen && hasChildren && (
          <motion.div initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2 }} className="overflow-hidden">
            {children.map((c) => (
              <TreeNode key={c.id} node={c} folders={folders} depth={depth + 1}
                currentId={currentId} onSelect={onSelect} expanded={expanded} toggle={toggle} />
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

export function FolderTreeView({ folders, documents }: { folders: FolderNode[]; documents: DocumentItem[] }) {
  // Root = the first folder with no parent (falls back to the first folder).
  const root = useMemo(
    () => folders.find((f) => f.parentId === null) ?? folders[0],
    [folders],
  );

  // Pre-expand the root and its direct children so the hierarchy is visible on open.
  const initialExpanded = useMemo(() => {
    if (!root) return new Set<string>();
    const ids = new Set<string>([root.id]);
    folders.filter((f) => f.parentId === root.id).forEach((f) => ids.add(f.id));
    return ids;
  }, [folders, root]);

  const [currentId, setCurrentId] = useState(root?.id ?? '');
  const [expanded, setExpanded] = useState<Set<string>>(initialExpanded);

  const toggle = (id: string) =>
    setExpanded((s) => { const n = new Set(s); if (n.has(id)) n.delete(id); else n.add(id); return n; });

  const activeId = currentId || root?.id || '';

  const breadcrumb = useMemo(() => {
    const chain: FolderNode[] = [];
    let cur: FolderNode | undefined = folders.find((f) => f.id === activeId);
    while (cur) { chain.unshift(cur); cur = folders.find((f) => f.id === cur!.parentId); }
    return chain;
  }, [activeId, folders]);

  const subFolders = folders.filter((f) => f.parentId === activeId);
  const docsHere = documents.filter((d) => d.folderId === activeId);

  // Selecting a folder makes it current and ensures it's expanded (never collapses on select).
  function openFolder(id: string) {
    setCurrentId(id);
    setExpanded((s) => new Set(s).add(id));
  }

  if (!root) {
    return (
      <EmptyState
        icon={<Folder className="h-6 w-6" />}
        title="No folders yet"
        hint="Folders will appear here once your workspace has a folder structure."
      />
    );
  }

  return (
    <div className="grid grid-cols-1 gap-5 lg:grid-cols-[260px_1fr]">
      {/* Tree */}
      <div className="surface h-fit p-3">
        <p className="mb-2 px-2 text-xs font-semibold uppercase tracking-wide text-ink-faint">Folders</p>
        <TreeNode node={root} folders={folders} depth={0} currentId={activeId} onSelect={openFolder} expanded={expanded} toggle={toggle} />
      </div>

      {/* Content */}
      <div className="space-y-4">
        {/* Breadcrumbs */}
        <div className="flex flex-wrap items-center gap-1 text-sm text-ink-muted">
          <Home className="h-4 w-4" />
          {breadcrumb.map((b, i) => (
            <span key={b.id} className="flex items-center gap-1">
              {i > 0 && <ChevronRight className="h-3.5 w-3.5 text-ink-faint" />}
              <button onClick={() => openFolder(b.id)}
                className={cn('rounded px-1.5 py-0.5 transition-colors hover:bg-slate-100 dark:hover:bg-[#1B232A]',
                  i === breadcrumb.length - 1 ? 'font-semibold text-ink dark:text-slate-100' : 'hover:text-ink')}>
                {b.name}
              </button>
            </span>
          ))}
        </div>

        <AnimatePresence mode="wait">
          <motion.div key={activeId} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -6 }}
            transition={{ duration: 0.2 }} className="space-y-4">
            {subFolders.length > 0 && (
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
                {subFolders.map((f) => (
                  <button key={f.id} onClick={() => openFolder(f.id)}
                    className="surface flex items-center gap-2.5 p-3.5 text-left transition-all hover:-translate-y-0.5 hover:shadow-soft">
                    <Folder className="h-5 w-5 shrink-0 text-brand-600" />
                    <span className="truncate text-sm font-medium text-ink dark:text-slate-100">{f.name}</span>
                  </button>
                ))}
              </div>
            )}

            {docsHere.length > 0 ? (
              <div className="space-y-2">
                {docsHere.map((d) => (
                  <div key={d.id} className="surface flex items-center gap-3 p-3.5">
                    <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-red-50 text-pdf dark:bg-red-950/30"><FileType2 className="h-5 w-5" /></span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-medium text-ink dark:text-slate-100">{d.name}</p>
                      <p className="truncate text-xs text-ink-muted">{d.type} • {fmtDayMonth(d.uploadedAt)}</p>
                    </div>
                    <StatusBadge status={d.status} />
                  </div>
                ))}
              </div>
            ) : subFolders.length === 0 ? (
              <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-line py-16 text-center dark:border-[#222A31]">
                <Folder className="mb-3 h-10 w-10 text-ink-faint" />
                <p className="text-sm font-medium text-ink dark:text-slate-100">This folder is empty</p>
                <p className="mt-1 text-sm text-ink-muted">Upload documents or create a subfolder.</p>
              </div>
            ) : null}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  );
}
