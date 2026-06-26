import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CheckSquare, Check, X, MessageSquare, FileType2 } from 'lucide-react';
import { Card, Button, Skeleton, EmptyState, Modal, Input } from '@/components/ui';
import { useToast } from '@/hooks';
import { AdminApi, DocumentsApi } from '@/services/api';
import { ApiError } from '@/config/api';

const errMsg = (e: unknown) => (e instanceof ApiError || e instanceof Error ? e.message : 'Something went wrong');
const QK = ['admin', 'approvals'];

export default function AdminApprovalQueuePage() {
  const qc = useQueryClient();
  const { push } = useToast();
  const { data, isLoading } = useQuery({ queryKey: QK, queryFn: () => AdminApi.approvals() });

  const [dialog, setDialog] = useState<{ id: string; name: string; mode: 'reject' | 'changes' } | null>(null);
  const [comment, setComment] = useState('');

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: QK });
    qc.invalidateQueries({ queryKey: ['documents'] });
  };

  const approve = useMutation({
    mutationFn: (id: string) => DocumentsApi.approve(id),
    onSuccess: () => { invalidate(); push({ title: 'Document approved', tone: 'success' }); },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  const review = useMutation({
    mutationFn: ({ id, mode, text }: { id: string; mode: 'reject' | 'changes'; text: string }) =>
      mode === 'reject' ? DocumentsApi.reject(id, text) : DocumentsApi.requestChanges(id, text),
    onSuccess: (_d, v) => {
      invalidate();
      setDialog(null); setComment('');
      push({ title: v.mode === 'reject' ? 'Document rejected' : 'Changes requested', tone: 'success' });
    },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-ink dark:text-slate-100">Approval queue</h2>
        <p className="text-sm text-ink-muted">Documents awaiting review across every project.</p>
      </div>

      {isLoading ? (
        <Skeleton className="h-64" />
      ) : !data || data.length === 0 ? (
        <EmptyState icon={<CheckSquare className="h-6 w-6" />} title="Nothing to review"
          hint="Newly uploaded documents will appear here for approval." />
      ) : (
        <Card className="divide-y divide-line p-0 dark:divide-[#222A31]">
          {data.map((d) => {
            const pending = (approve.isPending && approve.variables === d.documentId)
              || (review.isPending && review.variables?.id === d.documentId);
            return (
              <div key={d.documentId} className="flex flex-col gap-3 p-4 lg:flex-row lg:items-center">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-red-50 text-pdf dark:bg-red-950/30">
                  <FileType2 className="h-5 w-5" />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold text-ink dark:text-slate-100">{d.name}</p>
                  <p className="truncate text-xs text-ink-muted">
                    {d.uploadedByName ?? 'Unknown'} • {new Date(d.uploadedAt).toLocaleDateString()}
                    {d.projects ? ` • ${d.projects}` : ''}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Button size="sm" loading={pending} onClick={() => approve.mutate(d.documentId)}>
                    <Check className="h-4 w-4" /> Approve
                  </Button>
                  <Button variant="secondary" size="sm" disabled={pending}
                    onClick={() => { setComment(''); setDialog({ id: d.documentId, name: d.name, mode: 'changes' }); }}>
                    <MessageSquare className="h-4 w-4" /> Changes
                  </Button>
                  <Button variant="danger" size="sm" disabled={pending}
                    onClick={() => { setComment(''); setDialog({ id: d.documentId, name: d.name, mode: 'reject' }); }}>
                    <X className="h-4 w-4" /> Reject
                  </Button>
                </div>
              </div>
            );
          })}
        </Card>
      )}

      <Modal open={!!dialog} onClose={() => setDialog(null)}
        title={dialog?.mode === 'reject' ? 'Reject document' : 'Request changes'}>
        <form onSubmit={(e) => { e.preventDefault(); if (dialog) review.mutate({ id: dialog.id, mode: dialog.mode, text: comment }); }}
          className="space-y-4">
          <p className="text-sm text-ink-soft dark:text-slate-300">
            {dialog?.mode === 'reject' ? 'Reason for rejecting' : 'What needs to change'} <span className="font-medium">{dialog?.name}</span>?
          </p>
          <Input value={comment} onChange={(e) => setComment(e.target.value)}
            placeholder={dialog?.mode === 'reject' ? 'e.g. Wrong financial year' : 'e.g. Please re-upload a clearer scan'} autoFocus />
          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setDialog(null)}>Cancel</Button>
            <Button type="submit" variant={dialog?.mode === 'reject' ? 'danger' : 'primary'} loading={review.isPending}>
              {dialog?.mode === 'reject' ? 'Reject' : 'Send request'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
