import { motion } from 'framer-motion';
import { Folder, FileText, ArrowRight, Layers } from 'lucide-react';
import type { ProjectItem } from '@/types';
import { fmtDate, pluralize } from '@/lib/utils';

/**
 * Organize — STEP 1.
 * Centered, hover-animated project cards. Selecting one hands control to the
 * workspace (the parent fades these out and slides the table up).
 */
export function ProjectPicker({
  projects,
  onSelect,
}: {
  projects: ProjectItem[];
  onSelect: (p: ProjectItem) => void;
}) {
  return (
    <motion.div
      className="mx-auto max-w-5xl"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0, y: -12 }}
      transition={{ duration: 0.3 }}
    >
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: [0.16, 1, 0.3, 1] }}
        className="mb-10 text-center"
      >
        <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-glow">
          <Layers className="h-7 w-7" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">
          Organize a tender
        </h1>
        <p className="mx-auto mt-2 max-w-md text-sm text-ink-muted">
          Pick a project to map its requirements to documents. Drag, drop, and
          watch the package come together.
        </p>
      </motion.div>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {projects.map((p, i) => (
          <motion.button
            key={p.id}
            type="button"
            onClick={() => onSelect(p)}
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.05 * i, duration: 0.4, ease: [0.16, 1, 0.3, 1] }}
            whileHover={{ y: -6 }}
            whileTap={{ scale: 0.98 }}
            className="group surface relative overflow-hidden p-5 text-left transition-shadow hover:shadow-lift"
          >
            <div className="pointer-events-none absolute -right-10 -top-10 h-28 w-28 rounded-full bg-brand-50 opacity-0 transition-opacity duration-300 group-hover:opacity-100 dark:bg-brand-900/30" />
            <div className="relative flex items-start justify-between">
              <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 transition-colors group-hover:bg-brand-600 group-hover:text-white dark:bg-brand-900/40 dark:text-brand-200">
                <Folder className="h-6 w-6" />
              </span>
              <ArrowRight className="h-5 w-5 translate-x-0 text-ink-faint opacity-0 transition-all duration-300 group-hover:translate-x-1 group-hover:text-brand-600 group-hover:opacity-100" />
            </div>
            <h3 className="relative mt-4 text-base font-semibold text-ink dark:text-slate-100">
              {p.name}
            </h3>
            <p className="relative mt-1 line-clamp-1 text-sm text-ink-muted">
              {p.description || 'No description'}
            </p>
            <div className="relative mt-4 flex items-center gap-1.5 text-xs text-ink-muted">
              <FileText className="h-3.5 w-3.5" />
              {pluralize(p.documentIds.length, 'document')}
              <span className="mx-1.5 text-ink-faint">•</span>
              {fmtDate(p.createdAt)}
            </div>
          </motion.button>
        ))}
      </div>
    </motion.div>
  );
}
