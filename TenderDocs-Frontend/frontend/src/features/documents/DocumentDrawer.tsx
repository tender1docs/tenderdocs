import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { motion } from 'framer-motion';
import { useQueryClient } from '@tanstack/react-query';
import { X, FileType2, Download, Check, Ban } from 'lucide-react';
import { Button, Input, Select, StatusBadge, ApprovalBadge } from '@/components/ui';
import { useProjects, useToast } from '@/hooks';
import { useAuth } from '@/auth/AuthProvider';
import { useConfirm } from '@/components/ui/confirm';
import { can, Permission } from '@/lib/access';
import { apiClients, saveBlob } from '@/services';
import { DOCUMENT_CATEGORIES } from '@/types';
import type { DocumentItem, ApprovalStatus } from '@/types';
import { cn, financialYearOptions } from '@/lib/utils';

/* ---- global provider so any document click anywhere opens the same drawer ---- */
type DrawerCtx = { open: (doc: DocumentItem) => void; close: () => void };
const Ctx = createContext<DrawerCtx>({ open: () => {}, close: () => {} });
export const useDocumentDrawer = () => useContext(Ctx);

export function DocumentDrawerProvider({ children }: { children: ReactNode }) {
  const [doc, setDoc] = useState<DocumentItem | null>(null);
  return (
    <Ctx.Provider value={{ open: setDoc, close: () => setDoc(null) }}>
      {children}
      <DocumentDrawer doc={doc} onClose={() => setDoc(null)} />
    </Ctx.Provider>
  );
}

const PREVIEWABLE = ['pdf', 'png', 'jpg', 'jpeg', 'svg'];
const extOf = (name: string) => name.split('.').pop()?.toLowerCase() ?? '';

// Module-level LRU cache of preview blobs, keyed by document id. Reopening a preview then
// serves from memory instead of re-downloading the file from storage (Google Drive/local).
// Documents are immutable once uploaded (a new upload gets a new id), so this never goes stale.
const PREVIEW_CACHE = new Map<string, Blob>();
const PREVIEW_CACHE_MAX = 8;
function previewCacheGet(id: string): Blob | undefined {
  const b = PREVIEW_CACHE.get(id);
  if (b) { PREVIEW_CACHE.delete(id); PREVIEW_CACHE.set(id, b); } // refresh recency
  return b;
}
function previewCacheSet(id: string, b: Blob) {
  PREVIEW_CACHE.set(id, b);
  while (PREVIEW_CACHE.size > PREVIEW_CACHE_MAX) {
    const oldest = PREVIEW_CACHE.keys().next().value;
    if (oldest === undefined) break;
    PREVIEW_CACHE.delete(oldest);
  }
}
const clean = (v?: string | null) => (!v || v === '—' ? '' : v);

// Tender Reference & Custom Category have no dedicated column yet, so they round-trip as
// prefixed tags (ref:… / cat:…). Regular tags are kept separate.
function splitTags(tags: string[]) {
  let ref = '', cat = '', desc = '';
  const rest: string[] = [];
  for (const t of tags) {
    if (t.startsWith('ref:')) ref = t.slice(4);
    else if (t.startsWith('cat:')) cat = t.slice(4);
    else if (t.startsWith('desc:')) desc = t.slice(5);
    else rest.push(t);
  }
  return { ref, cat, desc, rest };
}
function mergeTags(rest: string[], ref: string, cat: string, desc: string) {
  const out = [...rest];
  if (ref.trim()) out.push(`ref:${ref.trim()}`);
  if (cat.trim()) out.push(`cat:${cat.trim()}`);
  if (desc.trim()) out.push(`desc:${desc.trim()}`);
  return out;
}

function DocumentDrawer({ doc, onClose }: { doc: DocumentItem | null; onClose: () => void }) {
  const qc = useQueryClient();
  const { push } = useToast();
  const confirm = useConfirm();
  const { data: projects = [] } = useProjects();
  const { permissions } = useAuth();
  const canApprove = can(permissions, Permission.DocumentsApprove);
  const canEdit = can(permissions, Permission.DocumentsEdit);

  const [tab, setTab] = useState<'metadata' | 'preview'>('metadata');
  const [saving, setSaving] = useState(false);

  // approval state (mirrors the doc, updated optimistically on approve/reject)
  const [approval, setApproval] = useState<ApprovalStatus>('pending');
  const [approvedBy, setApprovedBy] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState<string | null>(null);
  const [rejecting, setRejecting] = useState(false);
  const [rejectInput, setRejectInput] = useState('');
  const [reviewBusy, setReviewBusy] = useState(false);

  // editable fields
  const [name, setName] = useState('');
  const [category, setCategory] = useState('Other');
  const [authority, setAuthority] = useState('');
  const [fy, setFy] = useState('');
  const [expiry, setExpiry] = useState('');
  const [tenderRef, setTenderRef] = useState('');
  const [customCat, setCustomCat] = useState('');
  const [othersDesc, setOthersDesc] = useState('');
  const [tags, setTags] = useState<string[]>([]);
  const fyOptions = financialYearOptions();
  const [tagInput, setTagInput] = useState('');
  const [notes, setNotes] = useState('');

  // preview
  const [url, setUrl] = useState<string | null>(null);
  const [pvLoading, setPvLoading] = useState(false);
  const [pvFailed, setPvFailed] = useState(false);

  useEffect(() => {
    if (!doc) return;
    setTab('metadata');
    setName(doc.name);
    setCategory(doc.category && DOCUMENT_CATEGORIES.some((c) => c.value === doc.category) ? doc.category : 'Other');
    setAuthority(clean(doc.authority));
    setFy(clean(doc.financialYear));
    setExpiry(doc.expiryDate ? doc.expiryDate.slice(0, 10) : '');
    const { ref, cat, desc, rest } = splitTags(doc.tags ?? []);
    setTenderRef(ref); setCustomCat(cat); setOthersDesc(desc); setTags(rest);
    setNotes(doc.notes ?? '');
    setApproval(doc.approval);
    setApprovedBy(doc.approvedBy ?? null);
    setRejectionReason(doc.rejectionReason ?? null);
    setRejecting(false); setRejectInput('');
  }, [doc?.id]);

  async function review(decision: 'approve' | 'reject') {
    if (!doc) return;
    setReviewBusy(true);
    try {
      if (decision === 'approve') {
        await apiClients.DocumentsApi.approve(doc.id);
        setApproval('approved'); setRejectionReason(null);
      } else {
        await apiClients.DocumentsApi.reject(doc.id, rejectInput.trim() || undefined);
        setApproval('rejected'); setRejectionReason(rejectInput.trim() || null);
      }
      setRejecting(false); setRejectInput('');
      await Promise.all([
        qc.invalidateQueries({ queryKey: ['documents'] }),
        qc.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
      push({ title: decision === 'approve' ? 'Document approved' : 'Document rejected', tone: decision === 'approve' ? 'success' : 'danger' });
    } catch (err) {
      push({ title: (err as Error)?.message || 'Could not update', tone: 'danger' });
    } finally {
      setReviewBusy(false);
    }
  }

  // load preview when the tab opens — cancelable so a slow fetch can't set state on a stale doc.
  // Served instantly from the in-memory cache on reopen; only the first view hits the network.
  useEffect(() => {
    if (!(doc && tab === 'preview' && PREVIEWABLE.includes(extOf(doc.name)))) return;
    let ignore = false;
    let objUrl: string | null = null;
    const show = (b: Blob) => { if (ignore) return; objUrl = URL.createObjectURL(b); setUrl(objUrl); };

    const cached = previewCacheGet(doc.id);
    if (cached) {
      setPvFailed(false); setPvLoading(false); setUrl(null);
      show(cached);
    } else {
      setPvLoading(true); setPvFailed(false); setUrl(null);
      apiClients.DocumentsApi.download(doc.id)
        .then((b) => { if (ignore) return; previewCacheSet(doc.id, b); show(b); })
        .catch(() => { if (!ignore) setPvFailed(true); })
        .finally(() => { if (!ignore) setPvLoading(false); });
    }
    return () => { ignore = true; if (objUrl) URL.revokeObjectURL(objUrl); };
  }, [doc?.id, tab]);

  const usedIn = useMemo(
    () => (doc ? projects.filter((p) => p.documentIds.includes(doc.id)) : []),
    [projects, doc],
  );

  function addTag() {
    const t = tagInput.trim();
    if (t && !tags.includes(t)) setTags((p) => [...p, t]);
    setTagInput('');
  }

  async function save() {
    if (!doc) return;
    setSaving(true);
    try {
      await apiClients.DocumentsApi.update(doc.id, {
        name: name.trim() || doc.name,
        documentType: category,
        issuingAuthority: authority.trim(),
        financialYear: fy.trim(),
        expiryDate: expiry || undefined,
        notes: notes,
        tags: mergeTags(tags, tenderRef, customCat, category === 'Other' ? othersDesc : ''),
      });
      await Promise.all([
        qc.invalidateQueries({ queryKey: ['documents'] }),
        qc.invalidateQueries({ queryKey: ['projects'] }),
        qc.invalidateQueries({ queryKey: ['project'] }),
        qc.invalidateQueries({ queryKey: ['organize-project'] }),
        qc.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
      push({ title: 'Saved', tone: 'success' });
    } catch (err) {
      push({ title: (err as Error)?.message || 'Could not save', tone: 'danger' });
    } finally {
      setSaving(false);
    }
  }

  async function download() {
    if (!doc) return;
    try { saveBlob(await apiClients.DocumentsApi.download(doc.id), doc.name); }
    catch { push({ title: 'Download failed', tone: 'danger' }); }
  }

  const ext = doc ? extOf(doc.name) : '';
  const canPreview = PREVIEWABLE.includes(ext);

  if (!doc) return null;
  return createPortal(
    <div className="fixed inset-0 z-[70]">
      {/* backdrop — solid (no backdrop-blur, which can freeze when composited over a PDF iframe) */}
      <motion.div className="absolute inset-0 bg-slate-900/50"
        initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ duration: 0.15 }} onClick={onClose} />
      {/* panel — no exit animation so it (and the preview iframe) unmount instantly on close */}
      <motion.aside
        className="absolute inset-y-0 right-0 flex w-full max-w-[460px] flex-col bg-white shadow-2xl dark:bg-[#0E1419]"
        initial={{ x: '100%' }} animate={{ x: 0 }} transition={{ duration: 0.28, ease: [0.32, 0.72, 0, 1] }}>
            {/* header */}
            <div className="flex items-start gap-3 border-b border-line p-5 dark:border-[#222A31]">
              <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-red-50 text-pdf dark:bg-red-950/30"><FileType2 className="h-5 w-5" /></span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-base font-semibold text-ink dark:text-slate-100" title={doc.name}>{doc.name}</p>
                <p className="truncate text-xs text-ink-muted">{(doc.contentType || ext.toUpperCase() || 'File')} · {doc.sizeKb} KB · {doc.uploader}</p>
              </div>
              <div className="flex shrink-0 flex-col items-end gap-1.5">
                <ApprovalBadge status={approval} />
                <StatusBadge status={doc.status} />
              </div>
              <button onClick={onClose} className="rounded-lg p-1.5 text-ink-faint hover:bg-slate-100 dark:hover:bg-[#1B232A]"><X className="h-4 w-4" /></button>
            </div>

            {/* tabs */}
            <div className="flex gap-1 border-b border-line px-5 pt-3 dark:border-[#222A31]">
              {(['metadata', 'preview'] as const).map((t) => (
                <button key={t} onClick={() => setTab(t)}
                  className={cn('relative px-3 py-2 text-sm font-medium capitalize transition-colors',
                    tab === t ? 'text-brand-700 dark:text-brand-300' : 'text-ink-muted hover:text-ink-soft')}>
                  {t}
                  {tab === t && <motion.span layoutId="drawer-tab" className="absolute inset-x-0 -bottom-px h-0.5 rounded-full bg-brand-600" />}
                </button>
              ))}
            </div>

            <div className="flex-1 overflow-y-auto p-5">
              {tab === 'metadata' ? (
                <div className="space-y-4">
                  {/* Approval summary */}
                  <div className="flex items-start gap-3 rounded-xl border border-line bg-slate-50/60 p-3 text-sm dark:border-[#222A31] dark:bg-[#12181D]">
                    <ApprovalBadge status={approval} />
                    <div className="min-w-0 text-xs text-ink-muted">
                      {approval === 'approved' && <p>Approved{approvedBy ? ` by ${approvedBy}` : ''}.</p>}
                      {approval === 'rejected' && <p>Rejected{approvedBy ? ` by ${approvedBy}` : ''}.{rejectionReason ? ` Reason: ${rejectionReason}` : ''}</p>}
                      {approval === 'pending' && <p>Awaiting an approver’s decision.</p>}
                    </div>
                  </div>
                  <Field label="Name"><Input value={name} onChange={(e) => setName(e.target.value)} disabled={!canEdit} /></Field>
                  <Field label="Requirement Category">
                    <Select value={category} onChange={(e) => setCategory(e.target.value)}>
                      {DOCUMENT_CATEGORIES.map((c) => <option key={c.value} value={c.value}>{c.label}</option>)}
                    </Select>
                  </Field>

                  {/* description shown only for the "Others" requirement category */}
                  {category === 'Other' && (
                    <Field label="Description">
                      <textarea value={othersDesc} onChange={(e) => setOthersDesc(e.target.value)} rows={2} disabled={!canEdit}
                        className="w-full rounded-xl border border-line bg-white px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-faint focus:border-brand-400 focus:ring-2 focus:ring-brand-100 disabled:opacity-60 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100"
                        placeholder="Describe this document" />
                    </Field>
                  )}

                  <div className="grid grid-cols-2 gap-3">
                    <Field label="Firm / Individual Name"><Input value={authority} onChange={(e) => setAuthority(e.target.value)} placeholder="—" /></Field>
                    <Field label="Financial Year">
                      <Select value={fy} onChange={(e) => setFy(e.target.value)}>
                        <option value="">N/A</option>
                        {fy && !fyOptions.includes(fy) && <option value={fy}>{fy}</option>}
                        {fyOptions.map((y) => <option key={y} value={y}>{y}</option>)}
                      </Select>
                    </Field>
                    <Field label="Expiry Date"><Input type="date" value={expiry} onChange={(e) => setExpiry(e.target.value)} /></Field>
                    <Field label="Branch / Place"><Input value={tenderRef} onChange={(e) => setTenderRef(e.target.value)} placeholder="e.g. Hyderabad" /></Field>
                    <Field label="Document Type"><Input value={customCat} onChange={(e) => setCustomCat(e.target.value)} placeholder="e.g. Pre-qualification" /></Field>
                  </div>

                  <Field label="Projects">
                    {usedIn.length ? (
                      <div className="flex flex-wrap gap-1.5">
                        {usedIn.map((p) => <span key={p.id} className="rounded-full bg-brand-600 px-2.5 py-1 text-xs font-medium text-white">{p.name}</span>)}
                      </div>
                    ) : <p className="text-sm text-ink-muted">Not used in any project yet.</p>}
                  </Field>

                  <Field label="Tags">
                    <div className="flex gap-2">
                      <Input value={tagInput} onChange={(e) => setTagInput(e.target.value)}
                        onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addTag(); } }} placeholder="Add tag and press Enter" />
                      <Button variant="secondary" size="sm" onClick={addTag}>Add</Button>
                    </div>
                    {tags.length > 0 && (
                      <div className="mt-2 flex flex-wrap gap-1.5">
                        {tags.map((t) => (
                          <span key={t} className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-ink-soft dark:bg-[#1B232A]">
                            {t}<button onClick={() => setTags((p) => p.filter((x) => x !== t))} className="text-ink-faint hover:text-ink"><X className="h-3 w-3" /></button>
                          </span>
                        ))}
                      </div>
                    )}
                  </Field>

                  <Field label="Notes">
                    <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3}
                      className="w-full rounded-xl border border-line bg-white px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-faint focus:border-brand-400 focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100"
                      placeholder="Optional notes (visible & searchable)" />
                  </Field>
                </div>
              ) : (
                <div className="flex min-h-[320px] items-center justify-center overflow-hidden rounded-xl border border-line bg-slate-50 dark:border-[#222A31] dark:bg-[#0B1014]">
                  {!canPreview || pvFailed ? (
                    <div className="px-6 py-16 text-center">
                      <FileType2 className="mx-auto mb-3 h-10 w-10 text-ink-faint" />
                      <p className="text-sm font-medium text-ink dark:text-slate-100">Preview unavailable.</p>
                      <p className="mt-1 text-sm text-ink-muted">Please download the file.</p>
                    </div>
                  ) : pvLoading || !url ? (
                    <p className="py-16 text-sm text-ink-muted">Loading preview…</p>
                  ) : ext === 'pdf' ? (
                    <iframe key={doc.id} title={doc.name} src={url} className="h-[70vh] w-full" />
                  ) : (
                    <img src={url} alt={doc.name} className="max-h-[70vh] w-auto object-contain" />
                  )}
                </div>
              )}
            </div>

            {/* reject reason bar */}
            {canApprove && rejecting && (
              <div className="flex items-center gap-2 border-t border-line bg-danger-bg/40 px-4 py-3 dark:border-[#222A31]">
                <Input autoFocus value={rejectInput} onChange={(e) => setRejectInput(e.target.value)} placeholder="Reason for rejection (optional)" className="h-9" />
                <Button variant="secondary" size="sm" onClick={() => { setRejecting(false); setRejectInput(''); }}>Cancel</Button>
                <Button variant="danger" size="sm" loading={reviewBusy} onClick={() => review('reject')}>Confirm</Button>
              </div>
            )}

            {/* footer */}
            <div className="flex items-center justify-between gap-2 border-t border-line p-4 dark:border-[#222A31]">
              <Button variant="ghost" size="sm" onClick={download}><Download className="h-4 w-4" /> Download</Button>
              <div className="flex items-center gap-2">
                {canApprove && !rejecting && (
                  <>
                    <Button variant="secondary" size="sm" onClick={() => setRejecting(true)} disabled={approval === 'rejected'}><Ban className="h-4 w-4" /> Reject</Button>
                    <Button size="sm" loading={reviewBusy} onClick={async () => {
                      if (await confirm({ title: 'Approve document?', message: `"${doc.name}" will be marked approved and moved to the master folder.`, confirmText: 'Approve' })) review('approve');
                    }} disabled={approval === 'approved'}><Check className="h-4 w-4" /> Approve</Button>
                  </>
                )}
                {tab === 'metadata' && canEdit && <Button size="sm" loading={saving} onClick={async () => {
                  if (await confirm({ title: 'Save changes?', message: `Update the details for "${doc.name}"?`, confirmText: 'Save' })) save();
                }}>Save changes</Button>}
              </div>
            </div>
          </motion.aside>
    </div>,
    document.body,
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-ink-soft dark:text-slate-300">{label}</span>
      {children}
    </label>
  );
}
