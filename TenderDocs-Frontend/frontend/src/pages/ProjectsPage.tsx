import { useState } from 'react';
import { Link } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';
import { FolderKanban, Plus, Trash2, ArrowRight } from 'lucide-react';
import { Button, Card, Input, Modal, EmptyState, IconButton } from '@/components/ui';
import { useProjects, useCreateProject, useDeleteProject, useToast } from '@/hooks';
import { fmtDate, pluralize } from '@/lib/utils';

export default function ProjectsPage() {
  const { data: projects = [], isLoading } = useProjects();
  const create = useCreateProject();
  const del = useDeleteProject();
  const { push } = useToast();
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [desc, setDesc] = useState('');

  function submit() {
    if (!name.trim()) return;
    create.mutate({ name: name.trim(), description: desc.trim() }, {
      onSuccess: () => { push({ title: 'Project created', tone: 'success' }); setOpen(false); setName(''); setDesc(''); },
    });
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-ink dark:text-slate-100">Projects</h1>
          <p className="mt-1 text-sm text-ink-muted">Bundle documents per tender. Export an entire project as a ZIP.</p>
        </div>
        <Button onClick={() => setOpen(true)}><Plus className="h-4 w-4" /> New Project</Button>
      </div>

      {!isLoading && projects.length === 0 ? (
        <EmptyState icon={<FolderKanban className="h-6 w-6" />} title="No projects yet" hint="Create a project to start bundling tender documents."
          action={<Button onClick={() => setOpen(true)}><Plus className="h-4 w-4" /> New Project</Button>} />
      ) : (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-3">
          <AnimatePresence>
            {projects.map((p) => (
              <motion.div key={p.id} layout initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, scale: 0.97 }}>
                <Card className="flex h-full flex-col p-5 transition-all hover:-translate-y-0.5 hover:shadow-soft">
                  <div className="flex items-start gap-3">
                    <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-slate-100 text-ink-soft dark:bg-[#1B232A]"><FolderKanban className="h-5 w-5" /></span>
                    <div className="min-w-0">
                      <p className="truncate font-semibold text-ink dark:text-slate-100">{p.name}</p>
                      <p className="truncate text-sm text-ink-muted">{p.description || 'No description'}</p>
                    </div>
                  </div>
                  <div className="my-4 border-t border-line dark:border-[#1F262C]" />
                  <div className="mt-auto flex items-center justify-between">
                    <span className="text-sm text-ink-muted">{pluralize(p.documentIds.length, 'doc')} • created {fmtDate(p.createdAt)}</span>
                    <div className="flex items-center gap-1">
                      <IconButton className="text-danger-text hover:bg-danger-bg"
                        onClick={() => del.mutate(p.id, { onSuccess: () => push({ title: 'Project deleted', tone: 'danger' }) })}>
                        <Trash2 className="h-4 w-4" />
                      </IconButton>
                      <Link to={`/projects/${p.id}`}>
                        <Button variant="soft" size="sm">Open <ArrowRight className="h-4 w-4" /></Button>
                      </Link>
                    </div>
                  </div>
                </Card>
              </motion.div>
            ))}
          </AnimatePresence>
        </div>
      )}

      <Modal open={open} onClose={() => setOpen(false)} title="New project">
        <div className="space-y-4">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-ink-soft dark:text-slate-300">Project name</label>
            <Input value={name} autoFocus onChange={(e) => setName(e.target.value)} placeholder="e.g., METRO PROJECT" onKeyDown={(e) => e.key === 'Enter' && submit()} />
          </div>
          <div>
            <label className="mb-1.5 block text-sm font-medium text-ink-soft dark:text-slate-300">Description <span className="text-ink-faint">(optional)</span></label>
            <Input value={desc} onChange={(e) => setDesc(e.target.value)} placeholder="Short tender summary" />
          </div>
          <div className="flex justify-end gap-2 pt-1">
            <Button variant="secondary" onClick={() => setOpen(false)}>Cancel</Button>
            <Button onClick={submit} loading={create.isPending} disabled={!name.trim()}>Create project</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
