import { useMemo, useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ArrowLeft, FolderKanban, Plus, Upload as UploadIcon, Download, FileType2, Trash2,
  CheckCircle2, AlertTriangle, Package, Clock, FolderArchive, Network,
} from 'lucide-react';
import { Button, Card, StatusBadge, Modal } from '@/components/ui';
import { UploadDialog } from '@/components/layout/UploadDialog';
import {
  useProject, useDocuments, useSetProjectDocuments, useToast,
} from '@/hooks';
import { apiClients, saveBlob } from '@/services';
import { fmtDate, pluralize } from '@/lib/utils';
import { requirementTemplate } from '@/services/seed';

const ZIP_FOLDERS = ['GST', 'PAN', 'IT Returns', 'Financial', 'Technical', 'Others'];

export default function ProjectDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { data: project } = useProject(id);
  const { data: allDocs = [] } = useDocuments();
  const setDocs = useSetProjectDocuments();
  const { push } = useToast();
  const [upload, setUpload] = useState(false);
  const [addOpen, setAddOpen] = useState(false);
  const [zipOpen, setZipOpen] = useState(false);
  const [zipPhase, setZipPhase] = useState<'building' | 'done'>('building');

  const docs = useMemo(
    () => (project ? allDocs.filter((d) => project.documentIds.includes(d.id)) : []),
    [project, allDocs],
  );
  const available = useMemo(
    () => (project ? allDocs.filter((d) => !project.documentIds.includes(d.id)) : []),
    [project, allDocs],
  );

  const completion = Math.min(100, Math.round((docs.length / requirementTemplate.length) * 100));
  const expiring = docs.filter((d) => d.status === 'expiring' || d.status === 'expired');
  const missing = requirementTemplate.filter(
    (req) => !docs.some((d) => d.type.toLowerCase().includes(req.toLowerCase()) || req.toLowerCase().includes(d.type.toLowerCase().split(' ')[0])),
  ).slice(0, 6);

  if (!project) {
    return <div className="py-20 text-center text-sm text-ink-muted">Loading project…</div>;
  }

  function removeDoc(docId: string) {
    setDocs.mutate({ id: project!.id, documentIds: project!.documentIds.filter((x) => x !== docId) },
      { onSuccess: () => push({ title: 'Document removed', tone: 'danger' }) });
  }
  function addDoc(docId: string) {
    setDocs.mutate({ id: project!.id, documentIds: [...project!.documentIds, docId] },
      { onSuccess: () => push({ title: 'Document added', tone: 'success' }) });
  }
  function downloadZip() {
    setZipPhase('building'); setZipOpen(true);
    apiClients.ProjectsApi.downloadZip(project!.id)
      .then((blob) => {
        saveBlob(blob, `${project!.name}.zip`);
        setZipPhase('done'); push({ title: 'Package ready', tone: 'success' });
      })
      .catch(() => {
        setZipPhase('done'); push({ title: 'Could not generate ZIP', tone: 'danger' });
      });
  }

  return (
    <div className="space-y-5">
      <Link to="/projects" className="inline-flex items-center gap-2 text-sm font-medium text-ink-soft hover:text-ink dark:text-slate-300">
        <ArrowLeft className="h-4 w-4" /> Back to projects
      </Link>

      {/* Hero header */}
      <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
        className="flex flex-col gap-5 rounded-2xl border border-line bg-gradient-to-br from-hero-from to-hero-to p-6 dark:border-[#1A2127] dark:from-[#10211E] dark:to-[#0E1A18] lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-start gap-4">
          <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-white/70 text-brand-600 shadow-card dark:bg-[#12181D]"><FolderKanban className="h-6 w-6" /></span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">{project.name}</h1>
            <p className="text-sm text-ink-soft dark:text-slate-300">{project.description || 'No description'}</p>
            <p className="mt-2 text-sm text-ink-muted"><span className="font-semibold text-ink dark:text-slate-100">{pluralize(docs.length, 'document')}</span>&nbsp;&nbsp;&nbsp;Created {fmtDate(project.createdAt)}</p>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="secondary" onClick={() => setAddOpen(true)}><Plus className="h-4 w-4" /> Add Existing</Button>
          <Button variant="secondary" onClick={() => setUpload(true)}><UploadIcon className="h-4 w-4" /> Upload New</Button>
          <Button variant="secondary" onClick={() => navigate(`/projects/${project!.id}/organize`)}><Network className="h-4 w-4" /> Organize Documents</Button>
          <Button onClick={downloadZip}><Download className="h-4 w-4" /> Download ZIP</Button>
        </div>
      </motion.div>

      {/* Workspace stats */}
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-3">
        <Card className="p-5 lg:col-span-2">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="flex items-center gap-2 font-semibold text-ink dark:text-slate-100"><Package className="h-5 w-5 text-brand-600" /> Package completion</h3>
            <span className="text-sm font-semibold text-brand-600">{completion}%</span>
          </div>
          <div className="h-2.5 overflow-hidden rounded-full bg-slate-100 dark:bg-[#1B232A]">
            <motion.div className="h-full rounded-full bg-brand-600" initial={{ width: 0 }} animate={{ width: `${completion}%` }} transition={{ duration: 0.7, ease: 'easeOut' }} />
          </div>
          {missing.length > 0 && (
            <div className="mt-4">
              <p className="mb-2 text-sm font-medium text-ink-soft dark:text-slate-300">Missing requirements</p>
              <div className="flex flex-wrap gap-1.5">
                {missing.map((m) => <span key={m} className="rounded-full bg-warn-bg px-2.5 py-1 text-xs font-medium text-warn-text">{m}</span>)}
              </div>
            </div>
          )}
          <button onClick={() => navigate(`/projects/${project!.id}/organize`)} className="mt-4 inline-flex items-center gap-1.5 text-sm font-medium text-brand-600 hover:text-brand-700">
            <Network className="h-4 w-4" /> Organize documents in this project
          </button>
        </Card>

        <Card className="p-5">
          <h3 className="mb-3 flex items-center gap-2 font-semibold text-ink dark:text-slate-100"><AlertTriangle className="h-5 w-5 text-warn-text" /> Expiry alerts</h3>
          {expiring.length === 0 ? (
            <p className="py-6 text-center text-sm text-ink-muted">All documents valid. 🎉</p>
          ) : (
            <ul className="space-y-2">
              {expiring.map((d) => (
                <li key={d.id} className="flex items-center justify-between gap-2">
                  <span className="truncate text-sm text-ink-soft dark:text-slate-300">{d.name}</span>
                  <StatusBadge status={d.status} />
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {/* Documents */}
      <div className="space-y-2.5">
        {docs.length === 0 ? (
          <Card className="flex flex-col items-center py-12 text-center">
            <FileType2 className="mb-3 h-9 w-9 text-ink-faint" />
            <p className="text-sm font-medium text-ink dark:text-slate-100">No documents in this project</p>
            <p className="mt-1 text-sm text-ink-muted">Add existing documents or upload new ones.</p>
          </Card>
        ) : docs.map((d) => (
          <motion.div key={d.id} layout className="surface flex items-center gap-3 p-3.5">
            <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-red-50 text-pdf dark:bg-red-950/30"><FileType2 className="h-5 w-5" /></span>
            <div className="min-w-0 flex-1">
              <p className="truncate font-medium text-ink dark:text-slate-100">{d.name}</p>
              <p className="truncate text-sm text-ink-muted">{d.type} • {d.authority} • {d.financialYear}</p>
            </div>
            <StatusBadge status={d.status} />
            <button onClick={() => removeDoc(d.id)} className="rounded-lg p-2 text-danger-text hover:bg-danger-bg"><Trash2 className="h-4 w-4" /></button>
          </motion.div>
        ))}
      </div>

      {/* Activity timeline */}
      <Card className="p-6">
        <h3 className="mb-4 flex items-center gap-2 font-semibold text-ink dark:text-slate-100"><Clock className="h-5 w-5 text-brand-600" /> Activity</h3>
        <ol className="relative space-y-5 border-l border-line pl-5 dark:border-[#1F262C]">
          {[
            { t: 'Project created', d: fmtDate(project.createdAt), Icon: FolderKanban },
            ...docs.slice(0, 3).map((d) => ({ t: `Linked ${d.name}`, d: fmtDate(d.uploadedAt), Icon: FileType2 })),
          ].map((e, i) => (
            <li key={i} className="relative">
              <span className="absolute -left-[27px] flex h-5 w-5 items-center justify-center rounded-full bg-brand-50 text-brand-600 ring-4 ring-white dark:bg-brand-900/40 dark:ring-[#12181D]"><e.Icon className="h-3 w-3" /></span>
              <p className="text-sm font-medium text-ink dark:text-slate-100">{e.t}</p>
              <p className="text-xs text-ink-muted">{e.d}</p>
            </li>
          ))}
        </ol>
      </Card>

      {/* Add existing modal */}
      <Modal open={addOpen} onClose={() => setAddOpen(false)} title="Add existing documents" width="max-w-lg">
        <div className="max-h-80 space-y-2 overflow-y-auto">
          {available.length === 0 ? <p className="py-8 text-center text-sm text-ink-muted">Every document is already in this project.</p> :
            available.map((d) => (
              <div key={d.id} className="flex items-center gap-3 rounded-xl border border-line p-3 dark:border-[#1F262C]">
                <FileType2 className="h-5 w-5 text-pdf" />
                <div className="min-w-0 flex-1"><p className="truncate text-sm font-medium text-ink dark:text-slate-100">{d.name}</p><p className="truncate text-xs text-ink-muted">{d.type}</p></div>
                <Button size="sm" variant="soft" onClick={() => addDoc(d.id)}><Plus className="h-4 w-4" /> Add</Button>
              </div>
            ))}
        </div>
      </Modal>

      {/* ZIP generation modal */}
      <Modal open={zipOpen} onClose={() => setZipOpen(false)} title="Generate package" width="max-w-md">
        {zipPhase === 'building' ? (
          <div className="flex flex-col items-center py-8">
            <motion.div animate={{ rotate: 360 }} transition={{ repeat: Infinity, duration: 1.2, ease: 'linear' }}
              className="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-900/40"><FolderArchive className="h-7 w-7" /></motion.div>
            <p className="text-sm font-medium text-ink dark:text-slate-100">Building {project.name} package…</p>
            <p className="mt-1 text-xs text-ink-muted">Sorting documents into folders</p>
          </div>
        ) : (
          <div className="py-2">
            <div className="mb-4 flex items-center gap-3">
              <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-valid-bg text-valid-text"><CheckCircle2 className="h-5 w-5" /></span>
              <div><p className="text-sm font-semibold text-ink dark:text-slate-100">{project.name}.zip is ready</p><p className="text-xs text-ink-muted">Streamed to your browser</p></div>
            </div>
            <div className="rounded-xl border border-line bg-slate-50/60 p-3 font-mono text-xs text-ink-soft dark:border-[#1F262C] dark:bg-[#12181D] dark:text-slate-300">
              <p className="font-semibold">{project.name}/</p>
              {ZIP_FOLDERS.map((f) => <p key={f} className="pl-3">└── {f}/</p>)}
            </div>
            <div className="mt-5 flex justify-end"><Button onClick={() => setZipOpen(false)}>Done</Button></div>
          </div>
        )}
      </Modal>

      <UploadDialog open={upload} onClose={() => setUpload(false)} projectId={project.id} />
    </div>
  );
}
