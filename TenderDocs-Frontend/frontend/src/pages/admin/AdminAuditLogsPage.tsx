import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ScrollText } from 'lucide-react';
import { Card, Select, Badge, Skeleton, EmptyState, Button } from '@/components/ui';
import { AdminApi } from '@/services/api';

const ACTIONS = ['Login', 'Create', 'Update', 'Delete', 'Upload', 'Assign', 'Download', 'Export'];

const ACTION_TONE: Record<string, string> = {
  Login: 'bg-slate-100 text-ink-soft dark:bg-[#1B232A] dark:text-slate-300',
  Create: 'bg-valid-bg text-valid-text',
  Upload: 'bg-valid-bg text-valid-text',
  Update: 'bg-warn-bg text-warn-text',
  Assign: 'bg-warn-bg text-warn-text',
  Delete: 'bg-danger-bg text-danger-text',
};

export default function AdminAuditLogsPage() {
  const [action, setAction] = useState('');
  const [page, setPage] = useState(1);
  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'audit', action, page],
    queryFn: () => AdminApi.audit({ action: action || undefined, page, pageSize: 50 }),
  });

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold text-ink dark:text-slate-100">Audit logs</h2>
          <p className="text-sm text-ink-muted">Every recorded action in the workspace.</p>
        </div>
        <Select value={action} onChange={(e) => { setAction(e.target.value); setPage(1); }} className="w-44">
          <option value="">All actions</option>
          {ACTIONS.map((a) => <option key={a} value={a}>{a}</option>)}
        </Select>
      </div>

      {isLoading ? (
        <Skeleton className="h-96" />
      ) : !data || data.items.length === 0 ? (
        <EmptyState icon={<ScrollText className="h-6 w-6" />} title="No activity yet"
          hint="Actions like logins, uploads and approvals will appear here." />
      ) : (
        <>
          <Card className="overflow-x-auto p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-line text-left text-xs uppercase tracking-wide text-ink-muted dark:border-[#222A31]">
                  <th className="px-4 py-3 font-semibold">When</th>
                  <th className="px-4 py-3 font-semibold">Action</th>
                  <th className="px-4 py-3 font-semibold">Entity</th>
                  <th className="px-4 py-3 font-semibold">User</th>
                  <th className="px-4 py-3 font-semibold">Details</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((a) => (
                  <tr key={a.id} className="border-b border-line last:border-0 dark:border-[#222A31]">
                    <td className="whitespace-nowrap px-4 py-2.5 text-ink-soft dark:text-slate-300">
                      {new Date(a.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-2.5">
                      <Badge className={ACTION_TONE[a.action] ?? 'bg-slate-100 text-ink-soft dark:bg-[#1B232A] dark:text-slate-300'}>{a.action}</Badge>
                    </td>
                    <td className="px-4 py-2.5 text-ink-soft dark:text-slate-300">{a.entityType}</td>
                    <td className="px-4 py-2.5 text-ink-soft dark:text-slate-300">{a.userName ?? '—'}</td>
                    <td className="max-w-xs truncate px-4 py-2.5 text-xs text-ink-muted" title={a.detailsJson ?? ''}>
                      {a.detailsJson ?? ''}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          <div className="flex items-center justify-between text-sm text-ink-muted">
            <span>{data.totalCount} events</span>
            <div className="flex items-center gap-2">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <span>Page {data.page} / {totalPages}</span>
              <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
