import { useMemo, useState } from 'react';
import { AnimatePresence } from 'framer-motion';
import { Search, LayoutGrid, List, Plus, FolderTree as FolderTreeIcon, FileText } from 'lucide-react';
import { Button, Input, EmptyState } from '@/components/ui';
import { DocumentCard, DocumentRow } from '@/features/documents/DocumentCard';
import { FiltersPanel, emptyFilters, type DocFilters } from '@/features/documents/Filters';
import { CategoryFolders } from '@/features/documents/CategoryFolders';
import { UploadDialog } from '@/components/layout/UploadDialog';
import { useDocuments, useProjects, useDeleteDocument, useToast } from '@/hooks';
import { useAuth } from '@/auth/AuthProvider';
import { can } from '@/lib/access';
import type { ApprovalStatus } from '@/types';
import { cn } from '@/lib/utils';

const APPROVAL_TABS: { value: 'all' | ApprovalStatus; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'pending', label: 'Pending' },
  { value: 'approved', label: 'Approved' },
  { value: 'rejected', label: 'Rejected' },
];

export default function DocumentsPage() {
  const { data: documents = [] } = useDocuments();
  const { data: projects = [] } = useProjects();
  const del = useDeleteDocument();
  const { push } = useToast();
  const { role } = useAuth();
  const canUpload = !!role && can(role, 'upload');
  const canDelete = !!role && can(role, 'deleteDoc');

  const [query, setQuery] = useState('');
  const [view, setView] = useState<'grid' | 'list'>('grid');
  const [tab, setTab] = useState<'all' | 'folders'>('all');
  const [approval, setApproval] = useState<'all' | ApprovalStatus>('all');
  const [filters, setFilters] = useState<DocFilters>(emptyFilters);
  const [upload, setUpload] = useState(false);

  const options = useMemo(() => ({
    types: [...new Set(documents.map((d) => d.type))],
    authorities: [...new Set(documents.map((d) => d.authority))],
    fys: [...new Set(documents.map((d) => d.financialYear))],
    projects: projects.map((p) => p.name),
    uploaders: [...new Set(documents.map((d) => d.uploader))],
  }), [documents, projects]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    return documents.filter((d) => {
      if (q && ![d.name, d.type, d.authority, ...d.tags].join(' ').toLowerCase().includes(q)) return false;
      if (filters.type !== 'All' && d.type !== filters.type) return false;
      if (filters.authority !== 'All' && d.authority !== filters.authority) return false;
      if (filters.fy !== 'All' && d.financialYear !== filters.fy) return false;
      if (filters.uploader !== 'All' && d.uploader !== filters.uploader) return false;
      if (filters.expiry !== 'all' && d.status !== filters.expiry) return false;
      if (approval !== 'all' && d.approval !== approval) return false;
      if (filters.tag.trim() && !d.tags.some((t) => t.toLowerCase().includes(filters.tag.trim().toLowerCase()))) return false;
      if (filters.project !== 'All') {
        const p = projects.find((x) => x.name === filters.project);
        if (!p || !p.documentIds.includes(d.id)) return false;
      }
      return true;
    });
  }, [documents, query, filters, projects, approval]);

  function onDelete(id: string, name: string) {
    del.mutate(id, { onSuccess: () => push({ title: `${name} deleted`, tone: 'danger' }) });
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-ink dark:text-slate-100">Documents</h1>
          <p className="mt-0.5 text-sm text-ink-muted">{documents.length} {documents.length === 1 ? 'document' : 'documents'}</p>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-xl border border-line bg-white p-1 dark:border-[#222A31] dark:bg-[#12181D]">
            {([['grid', LayoutGrid], ['list', List]] as const).map(([v, Icon]) => (
              <button key={v} onClick={() => setView(v)}
                className={cn('rounded-lg p-1.5 transition-colors', view === v ? 'bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200' : 'text-ink-muted hover:text-ink')}>
                <Icon className="h-4 w-4" />
              </button>
            ))}
          </div>
          {canUpload && <Button onClick={() => setUpload(true)}><Plus className="h-4 w-4" /> Upload</Button>}
        </div>
      </div>

      {/* View tabs */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-1.5">
          {([['all', 'All documents', FileText], ['folders', 'Folders', FolderTreeIcon]] as const).map(([v, label, Icon]) => (
            <button key={v} onClick={() => setTab(v)}
              className={cn('inline-flex items-center gap-2 rounded-xl px-3.5 py-2 text-sm font-medium transition-colors',
                tab === v ? 'bg-brand-600 text-white' : 'bg-white text-ink-soft border border-line hover:bg-slate-50 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-300')}>
              <Icon className="h-4 w-4" /> {label}
            </button>
          ))}
        </div>
        {tab === 'all' && (
          <div className="flex flex-wrap items-center gap-1.5">
            {APPROVAL_TABS.map((a) => (
              <button key={a.value} onClick={() => setApproval(a.value)}
                className={cn('rounded-full px-3 py-1.5 text-xs font-medium transition-colors',
                  approval === a.value ? 'bg-brand-600 text-white'
                    : 'bg-slate-100 text-ink-soft hover:bg-slate-200 dark:bg-[#1B232A] dark:text-slate-300')}>
                {a.label}
              </button>
            ))}
          </div>
        )}
      </div>

      {tab === 'all' ? (
        <>
          <div className="relative">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 h-5 w-5 -translate-y-1/2 text-ink-faint" />
            <Input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search by name, content, tags, notes…" className="h-12 pl-11" />
          </div>

          <div className="grid grid-cols-1 gap-5 lg:grid-cols-[300px_1fr]">
            <FiltersPanel value={filters} onChange={setFilters} options={options} />
            <div>
              {filtered.length === 0 ? (
                <EmptyState icon={<FileText className="h-6 w-6" />} title="No documents match" hint="Try clearing filters or uploading a new document."
                  action={canUpload ? <Button onClick={() => setUpload(true)}><Plus className="h-4 w-4" /> Upload document</Button> : undefined} />
              ) : view === 'grid' ? (
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
                  <AnimatePresence>{filtered.map((d) => <DocumentCard key={d.id} doc={d} onDelete={canDelete ? () => onDelete(d.id, d.name) : undefined} />)}</AnimatePresence>
                </div>
              ) : (
                <div className="space-y-2.5"><AnimatePresence>{filtered.map((d) => <DocumentRow key={d.id} doc={d} onDelete={canDelete ? () => onDelete(d.id, d.name) : undefined} />)}</AnimatePresence></div>
              )}
            </div>
          </div>
        </>
      ) : (
        <CategoryFolders documents={documents} projects={projects} />
      )}

      <UploadDialog open={upload} onClose={() => setUpload(false)} />
    </div>
  );
}
