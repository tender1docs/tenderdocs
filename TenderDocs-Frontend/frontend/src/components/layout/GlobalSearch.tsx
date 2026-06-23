import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Search, FileText, FolderKanban, Folder, CornerDownLeft } from 'lucide-react';
import { useDocuments, useProjects, useFolders } from '@/hooks';
import { cn } from '@/lib/utils';

type Result =
  | { kind: 'document'; id: string; label: string; sub: string }
  | { kind: 'project'; id: string; label: string; sub: string }
  | { kind: 'folder'; id: string; label: string; sub: string };

export function GlobalSearch({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [q, setQ] = useState('');
  const [active, setActive] = useState(0);
  const navigate = useNavigate();
  const { data: documents = [] } = useDocuments();
  const { data: projects = [] } = useProjects();
  const { data: folders = [] } = useFolders();

  const results = useMemo<Result[]>(() => {
    const t = q.trim().toLowerCase();
    if (!t) return [];
    const out: Result[] = [];
    documents.forEach((d) => {
      if ([d.name, d.type, d.authority, ...d.tags].join(' ').toLowerCase().includes(t))
        out.push({ kind: 'document', id: d.id, label: d.name, sub: d.type });
    });
    projects.forEach((p) => {
      if ([p.name, p.description].join(' ').toLowerCase().includes(t))
        out.push({ kind: 'project', id: p.id, label: p.name, sub: 'Project' });
    });
    folders.forEach((f) => {
      if (f.name.toLowerCase().includes(t)) out.push({ kind: 'folder', id: f.id, label: f.name, sub: 'Folder' });
    });
    return out.slice(0, 8);
  }, [q, documents, projects, folders]);

  useEffect(() => { setActive(0); }, [q]);
  useEffect(() => { if (open) { setQ(''); setActive(0); } }, [open]);

  function go(r: Result) {
    onClose();
    if (r.kind === 'project') navigate(`/projects/${r.id}`);
    else if (r.kind === 'document') navigate('/documents');
    else navigate('/documents');
  }

  function onKey(e: React.KeyboardEvent) {
    if (e.key === 'ArrowDown') { e.preventDefault(); setActive((a) => Math.min(a + 1, results.length - 1)); }
    if (e.key === 'ArrowUp') { e.preventDefault(); setActive((a) => Math.max(a - 1, 0)); }
    if (e.key === 'Enter' && results[active]) { e.preventDefault(); go(results[active]); }
    if (e.key === 'Escape') onClose();
  }

  const iconFor = (k: Result['kind']) => k === 'document' ? FileText : k === 'project' ? FolderKanban : Folder;

  return (
    <AnimatePresence>
      {open && (
        <motion.div className="fixed inset-0 z-[70] flex items-start justify-center p-4 pt-[12vh]"
          initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
          <div className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm" onClick={onClose} />
          <motion.div
            initial={{ opacity: 0, y: -12, scale: 0.98 }} animate={{ opacity: 1, y: 0, scale: 1 }} exit={{ opacity: 0, y: -8 }}
            transition={{ type: 'spring', stiffness: 380, damping: 30 }}
            className="relative w-full max-w-xl overflow-hidden rounded-2xl bg-white shadow-lift dark:bg-[#12181D] dark:border dark:border-[#222A31]">
            <div className="flex items-center gap-3 border-b border-line px-4 dark:border-[#222A31]">
              <Search className="h-5 w-5 text-ink-muted" />
              <input
                autoFocus value={q} onChange={(e) => setQ(e.target.value)} onKeyDown={onKey}
                placeholder="Search projects, folders, documents, tags…"
                className="h-14 flex-1 bg-transparent text-[15px] text-ink outline-none placeholder:text-ink-faint dark:text-slate-100" />
              <kbd className="rounded-md border border-line px-1.5 py-0.5 text-[11px] text-ink-muted dark:border-[#222A31]">Esc</kbd>
            </div>
            <div className="max-h-80 overflow-y-auto p-2">
              {q && results.length === 0 && (
                <p className="px-3 py-8 text-center text-sm text-ink-muted">No matches for "{q}"</p>
              )}
              {!q && <p className="px-3 py-8 text-center text-sm text-ink-muted">Type to search across everything.</p>}
              {results.map((r, i) => {
                const Icon = iconFor(r.kind);
                return (
                  <button key={`${r.kind}-${r.id}`} onMouseEnter={() => setActive(i)} onClick={() => go(r)}
                    className={cn('flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left transition-colors',
                      active === i ? 'bg-brand-50 dark:bg-brand-900/40' : 'hover:bg-slate-50 dark:hover:bg-[#161D23]')}>
                    <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-100 text-ink-soft dark:bg-[#1B232A]"><Icon className="h-4 w-4" /></span>
                    <span className="flex-1">
                      <span className="block text-sm font-medium text-ink dark:text-slate-100">{r.label}</span>
                      <span className="block text-xs text-ink-muted">{r.sub}</span>
                    </span>
                    {active === i && <CornerDownLeft className="h-4 w-4 text-ink-faint" />}
                  </button>
                );
              })}
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
