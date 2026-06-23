import { motion } from 'framer-motion';
import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  Bell, CalendarClock, FolderArchive, Upload, Settings as Cog, CheckCheck,
} from 'lucide-react';
import type { NotificationItem } from '@/types';
import { useNotifications } from '@/hooks';
import { notificationService } from '@/services';
import { Button, Card, Skeleton, EmptyState } from '@/components/ui';
import { cn, fmtDate } from '@/lib/utils';

const kindMeta: Record<NotificationItem['kind'], { Icon: typeof Bell; cls: string }> = {
  expiry: { Icon: CalendarClock, cls: 'bg-warn-bg text-warn-text' },
  project: { Icon: FolderArchive, cls: 'bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-200' },
  upload: { Icon: Upload, cls: 'bg-valid-bg text-valid-text' },
  system: { Icon: Cog, cls: 'bg-slate-100 text-ink-soft dark:bg-[#1B232A] dark:text-slate-300' },
};

export default function NotificationsPage() {
  const { data, isLoading } = useNotifications();
  const qc = useQueryClient();
  const [busy, setBusy] = useState(false);

  const unread = data?.filter((n) => !n.read).length ?? 0;

  const markAll = async () => {
    setBusy(true);
    await notificationService.markAllRead();
    await qc.invalidateQueries({ queryKey: ['notifications'] });
    setBusy(false);
  };

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">Notifications</h1>
          <p className="mt-1 text-sm text-ink-muted">
            {unread > 0 ? `${unread} unread` : 'You are all caught up'}
          </p>
        </div>
        <Button variant="secondary" size="sm" onClick={markAll} loading={busy} disabled={unread === 0}>
          <CheckCheck className="h-4 w-4" /> Mark all read
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2.5">
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-20" />)}
        </div>
      ) : !data?.length ? (
        <EmptyState icon={<Bell className="h-6 w-6" />} title="No notifications" hint="Expiry alerts and updates will appear here." />
      ) : (
        <div className="space-y-2.5">
          {data.map((n, i) => {
            const { Icon, cls } = kindMeta[n.kind];
            return (
              <motion.div
                key={n.id}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.04, duration: 0.3 }}
              >
                <Card className={cn('flex items-start gap-3.5 p-4', !n.read && 'ring-1 ring-brand-100 dark:ring-brand-900/40')}>
                  <span className={cn('flex h-10 w-10 shrink-0 items-center justify-center rounded-xl', cls)}>
                    <Icon className="h-5 w-5" />
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-semibold text-ink dark:text-slate-100">{n.title}</p>
                      {!n.read && <span className="h-2 w-2 shrink-0 rounded-full bg-brand-600" />}
                    </div>
                    <p className="mt-0.5 text-sm text-ink-muted">{n.body}</p>
                  </div>
                  <span className="shrink-0 text-xs text-ink-faint">{fmtDate(n.createdAt)}</span>
                </Card>
              </motion.div>
            );
          })}
        </div>
      )}
    </div>
  );
}
