import { useRef, useState } from 'react';
import { motion } from 'framer-motion';
import { useQueryClient } from '@tanstack/react-query';
import { UploadCloud, CheckCircle2, FileText, X } from 'lucide-react';
import { Button, Modal, Input, Select } from '@/components/ui';
import { useConfirm } from '@/components/ui/confirm';
import { useToast } from '@/hooks';
import { apiClients } from '@/services';
import { DOCUMENT_CATEGORIES } from '@/types';
import { cn, financialYearOptions, financialYearForDate } from '@/lib/utils';

/**
 * Upload a document with full metadata. When `projectId` is supplied (uploading from inside a
 * project), the new document is created in the global library AND linked to that project.
 */
export function UploadDialog({
  open,
  onClose,
  projectId,
}: {
  open: boolean;
  onClose: () => void;
  projectId?: string;
}) {
  const qc = useQueryClient();
  const { push } = useToast();
  const confirm = useConfirm();
  const inputRef = useRef<HTMLInputElement>(null);

  const [drag, setDrag] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [phase, setPhase] = useState<'form' | 'uploading' | 'done'>('form');
  const [savings, setSavings] = useState<{ original: number; stored: number } | null>(null);

  // metadata
  const [category, setCategory] = useState('Gst');
  const [authority, setAuthority] = useState('');
  const [financialYear, setFinancialYear] = useState('');
  const [tags, setTags] = useState('');
  const [tenderRef, setTenderRef] = useState('');
  const [customCategory, setCustomCategory] = useState('');
  const [othersDesc, setOthersDesc] = useState('');
  const [notes, setNotes] = useState('');
  const [expiryRequired, setExpiryRequired] = useState(false);
  const [expiryDate, setExpiryDate] = useState('');

  const fyOptions = financialYearOptions();

  function reset() {
    setFile(null); setPhase('form'); setCategory('Gst'); setAuthority('');
    setFinancialYear(''); setTags(''); setTenderRef(''); setCustomCategory(''); setOthersDesc(''); setNotes('');
    setExpiryRequired(false); setExpiryDate('');
    setSavings(null);
  }

  // When an expiry date is picked, default the financial year to the one it falls in.
  function onExpiryDate(value: string) {
    setExpiryDate(value);
    if (value && !financialYear) {
      const fy = financialYearForDate(value);
      if (fy) setFinancialYear(fy);
    }
  }
  function close() { onClose(); setTimeout(reset, 200); }

  function pickFile(files: FileList | null) {
    const f = files?.[0];
    if (f) setFile(f);
  }

  function fmtBytes(n: number) {
    if (n >= 1024 * 1024) return `${(n / (1024 * 1024)).toFixed(1)} MB`;
    return `${Math.max(1, Math.round(n / 1024))} KB`;
  }

  async function submit() {
    if (!file) { push({ title: 'Choose a file first', tone: 'danger' }); return; }
    if (expiryRequired && !expiryDate) { push({ title: 'Set an expiry date', tone: 'danger' }); return; }

    const ok = await confirm({
      title: 'Upload this document?',
      message: `“${file.name}” will be added${projectId ? ' and linked to this project' : ' to your library'}.`,
      confirmText: 'Yes, upload',
      cancelText: 'No',
    });
    if (!ok) return;

    setPhase('uploading');
    try {
      const result = await apiClients.DocumentsApi.upload({
        file,
        documentType: category,
        issuingAuthority: authority.trim() || undefined,
        financialYear: financialYear.trim() || undefined,
        notes: notes.trim() || undefined,
        expiryDate: expiryRequired && expiryDate ? expiryDate : undefined,
        tags: (() => {
          const list = tags.split(',').map((t) => t.trim()).filter(Boolean);
          if (tenderRef.trim()) list.push(`ref:${tenderRef.trim()}`);
          if (customCategory.trim()) list.push(`cat:${customCategory.trim()}`);
          if (category === 'Other' && othersDesc.trim()) list.push(`desc:${othersDesc.trim()}`);
          return list.length ? list.join(',') : undefined;
        })(),
        projectId,
      });
      setSavings({ original: file.size, stored: result.fileSizeBytes });
      await qc.invalidateQueries({ queryKey: ['documents'] });
      await qc.invalidateQueries({ queryKey: ['projects'] });
      if (projectId) await qc.invalidateQueries({ queryKey: ['project', projectId] });
      setPhase('done');
      push({ title: `${file.name} uploaded`, tone: 'success' });
    } catch {
      setPhase('form');
      push({ title: 'Upload failed', tone: 'danger' });
    }
  }

  return (
    <Modal open={open} onClose={close} title={projectId ? 'Upload to project' : 'Upload document'} width="max-w-lg">
      {phase === 'done' ? (
        <div className="flex flex-col items-center py-8 text-center">
          <motion.div initial={{ scale: 0.6, opacity: 0 }} animate={{ scale: 1, opacity: 1 }}
            transition={{ type: 'spring', stiffness: 320, damping: 18 }}
            className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-valid-bg text-valid-text">
            <CheckCircle2 className="h-8 w-8" />
          </motion.div>
          <p className="text-base font-semibold text-ink dark:text-slate-100">Upload complete</p>
          <p className="mt-1 text-sm text-ink-muted">
            {file?.name} was added{projectId ? ' and linked to this project' : ' to your library'}.
          </p>
          {savings && savings.stored < savings.original && (
            <div className="mt-4 rounded-xl border border-valid-bg bg-valid-bg/40 px-4 py-2.5 text-sm text-valid-text">
              Compressed {fmtBytes(savings.original)} → <span className="font-semibold">{fmtBytes(savings.stored)}</span>
              {' '}({Math.round((1 - savings.stored / savings.original) * 100)}% smaller)
            </div>
          )}
          <div className="mt-6 flex gap-2">
            <Button variant="secondary" onClick={reset}>Upload another</Button>
            <Button onClick={close}>Done</Button>
          </div>
        </div>
      ) : phase === 'uploading' ? (
        <div className="flex flex-col items-center py-10">
          <motion.div animate={{ rotate: 360 }} transition={{ repeat: Infinity, duration: 1, ease: 'linear' }}
            className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-900/40">
            <UploadCloud className="h-6 w-6" />
          </motion.div>
          <p className="text-sm font-medium text-ink dark:text-slate-100">Uploading {file?.name}…</p>
        </div>
      ) : (
        <div className="space-y-4">
          {/* dropzone / chosen file */}
          {file ? (
            <div className="flex items-center gap-3 rounded-xl border border-line p-3 dark:border-[#222A31]">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-50 text-brand-600 dark:bg-brand-900/40"><FileText className="h-5 w-5" /></span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-ink dark:text-slate-100">{file.name}</p>
                <p className="text-xs text-ink-muted">{Math.max(1, Math.round(file.size / 1024))} KB</p>
              </div>
              <button onClick={() => setFile(null)} className="rounded-lg p-1.5 text-ink-faint hover:bg-slate-100 dark:hover:bg-[#1B232A]"><X className="h-4 w-4" /></button>
            </div>
          ) : (
            <div
              onDragOver={(e) => { e.preventDefault(); setDrag(true); }}
              onDragLeave={() => setDrag(false)}
              onDrop={(e) => { e.preventDefault(); setDrag(false); pickFile(e.dataTransfer.files); }}
              onClick={() => inputRef.current?.click()}
              className={cn('flex cursor-pointer flex-col items-center justify-center rounded-2xl border-2 border-dashed py-8 transition-colors',
                drag ? 'border-brand-500 bg-brand-50/60 dark:bg-brand-900/20' : 'border-line hover:border-brand-300 dark:border-[#222A31]')}>
              <UploadCloud className="mb-2 h-8 w-8 text-brand-500" />
              <p className="text-sm font-medium text-ink dark:text-slate-100">Drag & drop a file here</p>
              <p className="mt-1 text-xs text-ink-muted">or click to browse — PDF, DOCX, XLSX, images</p>
              <input ref={inputRef} type="file" className="hidden" onChange={(e) => pickFile(e.target.files)} />
            </div>
          )}

          {/* metadata */}
          <div className="space-y-3">
            <label className="block">
              <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Requirement category</span>
              <Select value={category} onChange={(e) => setCategory(e.target.value)}>
                {DOCUMENT_CATEGORIES.map((c) => <option key={c.value} value={c.value}>{c.label}</option>)}
              </Select>
            </label>

            {/* description shown only when "Others" is the requirement category */}
            {category === 'Other' && (
              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Description</span>
                <textarea value={othersDesc} onChange={(e) => setOthersDesc(e.target.value)} rows={2}
                  className="w-full rounded-xl border border-line bg-white px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-faint focus:border-brand-400 focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100"
                  placeholder="Describe this document" />
              </label>
            )}

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Firm / Individual name</span>
                <Input value={authority} onChange={(e) => setAuthority(e.target.value)} placeholder="e.g. ABC Constructions Pvt Ltd" />
              </label>
              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Financial year</span>
                <Select value={financialYear} onChange={(e) => setFinancialYear(e.target.value)}>
                  <option value="">N/A</option>
                  {fyOptions.map((fy) => <option key={fy} value={fy}>{fy}</option>)}
                </Select>
              </label>
              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Branch / Place</span>
                <Input value={tenderRef} onChange={(e) => setTenderRef(e.target.value)} placeholder="e.g. Hyderabad" />
              </label>
              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Document type</span>
                <Input value={customCategory} onChange={(e) => setCustomCategory(e.target.value)} placeholder="e.g. Pre-qualification" />
              </label>
              <label className="block sm:col-span-2">
                <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Tags (optional)</span>
                <Input value={tags} onChange={(e) => setTags(e.target.value)} placeholder="comma, separated" />
              </label>
            </div>
          </div>

          {/* expiry */}
          <div className="rounded-xl border border-line p-3 dark:border-[#222A31]">
            <label className="flex cursor-pointer items-center gap-2.5">
              <input type="checkbox" checked={expiryRequired} onChange={(e) => setExpiryRequired(e.target.checked)}
                className="h-4 w-4 rounded border-line text-brand-600 focus:ring-brand-500" />
              <span className="text-sm font-medium text-ink dark:text-slate-100">This document has an expiry date</span>
            </label>
            {expiryRequired && (
              <div className="mt-3">
                <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Expiry date</span>
                <Input type="date" value={expiryDate} onChange={(e) => onExpiryDate(e.target.value)} />
              </div>
            )}
          </div>

          {/* notes */}
          <label className="block">
            <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">Notes</span>
            <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2}
              className="w-full rounded-xl border border-line bg-white px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-faint focus:border-brand-400 focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100"
              placeholder="Optional notes (visible & searchable)" />
          </label>

          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={close}>Cancel</Button>
            <Button onClick={submit} disabled={!file}>Upload</Button>
          </div>
        </div>
      )}
    </Modal>
  );
}
