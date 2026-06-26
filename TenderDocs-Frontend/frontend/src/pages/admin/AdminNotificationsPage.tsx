import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, Send } from 'lucide-react';
import { Card, Button, Input, Select, Skeleton } from '@/components/ui';
import { useToast } from '@/hooks';
import { AdminApi, UsersApi } from '@/services/api';
import { ApiError } from '@/config/api';
import { cn } from '@/lib/utils';

const TARGETS = [
  { value: 'Everyone', label: 'Everyone' },
  { value: 'Admin', label: 'Admins' },
  { value: 'Uploader', label: 'Uploaders' },
  { value: 'Approver', label: 'Approvers' },
  { value: 'Viewer', label: 'Viewers' },
  { value: 'User', label: 'Specific user…' },
];

const errMsg = (e: unknown) => (e instanceof ApiError || e instanceof Error ? e.message : 'Something went wrong');

export default function AdminNotificationsPage() {
  const qc = useQueryClient();
  const { push } = useToast();

  const [target, setTarget] = useState('Everyone');
  const [userId, setUserId] = useState('');
  const [title, setTitle] = useState('');
  const [message, setMessage] = useState('');

  const { data: users } = useQuery({ queryKey: ['admin', 'users'], queryFn: () => UsersApi.list() });
  const { data: recent, isLoading } = useQuery({ queryKey: ['admin', 'notifications'], queryFn: () => AdminApi.notifications() });

  const broadcast = useMutation({
    mutationFn: () => AdminApi.broadcast({ target, userId: target === 'User' ? userId : undefined, title, message }),
    onSuccess: (r) => {
      qc.invalidateQueries({ queryKey: ['admin', 'notifications'] });
      setTitle(''); setMessage('');
      push({ title: `Sent to ${r.recipients} ${r.recipients === 1 ? 'person' : 'people'}`, tone: 'success' });
    },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  const valid = title.trim() && message.trim() && (target !== 'User' || userId);

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-ink dark:text-slate-100">Notifications</h2>
        <p className="text-sm text-ink-muted">Broadcast an announcement to people in the workspace.</p>
      </div>

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
        <Card className="space-y-4 p-5">
          <form onSubmit={(e) => { e.preventDefault(); if (valid) broadcast.mutate(); }} className="space-y-4">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">Audience</label>
                <Select value={target} onChange={(e) => setTarget(e.target.value)}>
                  {TARGETS.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                </Select>
              </div>
              {target === 'User' && (
                <div>
                  <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">User</label>
                  <Select value={userId} onChange={(e) => setUserId(e.target.value)}>
                    <option value="">Select…</option>
                    {(users ?? []).map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
                  </Select>
                </div>
              )}
            </div>
            <div>
              <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">Title</label>
              <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Submission deadline moved" />
            </div>
            <div>
              <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">Message</label>
              <textarea value={message} onChange={(e) => setMessage(e.target.value)} rows={4}
                placeholder="Write your announcement…"
                className={cn('w-full rounded-xl border border-line bg-white px-3.5 py-2.5 text-sm text-ink placeholder:text-ink-faint',
                  'focus:border-brand-400 focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100 dark:focus:ring-brand-900/40')} />
            </div>
            <div className="flex justify-end">
              <Button type="submit" loading={broadcast.isPending} disabled={!valid}>
                <Send className="h-4 w-4" /> Send
              </Button>
            </div>
          </form>
        </Card>

        <Card className="p-0">
          <div className="border-b border-line px-5 py-4 dark:border-[#222A31]">
            <h3 className="font-semibold text-ink dark:text-slate-100">Recent</h3>
          </div>
          {isLoading ? (
            <div className="space-y-2 p-5">{Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-12" />)}</div>
          ) : !recent || recent.length === 0 ? (
            <p className="px-5 py-10 text-center text-sm text-ink-muted">No notifications sent yet.</p>
          ) : (
            <div className="max-h-[460px] divide-y divide-line overflow-y-auto dark:divide-[#222A31]">
              {recent.map((n) => (
                <div key={n.id} className="flex gap-3 px-5 py-3">
                  <Bell className="mt-0.5 h-4 w-4 shrink-0 text-brand-500" />
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-ink dark:text-slate-100">{n.title}</p>
                    <p className="line-clamp-2 text-xs text-ink-muted">{n.message}</p>
                    <p className="mt-0.5 text-[11px] text-ink-faint">
                      {n.userName ?? 'User'} • {new Date(n.createdAt).toLocaleString()}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}
