import { useQuery } from '@tanstack/react-query';
import { Cloud, CloudOff, HardDrive, FileText, FolderKanban, CheckCircle2, type LucideIcon } from 'lucide-react';
import { Card, Badge, Skeleton } from '@/components/ui';
import { AdminApi } from '@/services/api';
import { cn } from '@/lib/utils';

function formatBytes(n: number): string {
  if (n < 1024) return `${n} B`;
  const units = ['KB', 'MB', 'GB', 'TB'];
  let v = n / 1024, i = 0;
  while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
  return `${v.toFixed(v < 10 ? 1 : 0)} ${units[i]}`;
}

function Stat({ icon: Icon, label, value }: { icon: LucideIcon; label: string; value: string }) {
  return (
    <Card className="flex items-center gap-4 p-5">
      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-300">
        <Icon className="h-5 w-5" />
      </span>
      <div className="min-w-0">
        <p className="text-sm text-ink-muted">{label}</p>
        <p className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">{value}</p>
      </div>
    </Card>
  );
}

export default function AdminStoragePage() {
  const { data, isLoading } = useQuery({ queryKey: ['admin', 'storage'], queryFn: () => AdminApi.storage() });

  return (
    <div className="space-y-5">
      <div>
        <h2 className="text-lg font-semibold text-ink dark:text-slate-100">Storage</h2>
        <p className="text-sm text-ink-muted">Where documents are stored and how much space they use.</p>
      </div>

      {isLoading || !data ? (
        <Skeleton className="h-40" />
      ) : (
        <>
          <Card className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-3.5">
              <span className={cn('flex h-11 w-11 items-center justify-center rounded-xl',
                data.googleDriveConnected ? 'bg-valid-bg text-valid-text' : 'bg-slate-100 text-ink-soft dark:bg-[#1B232A]')}>
                {data.googleDriveConnected ? <Cloud className="h-5 w-5" /> : <CloudOff className="h-5 w-5" />}
              </span>
              <div>
                <p className="font-semibold text-ink dark:text-slate-100">
                  {data.provider === 'GoogleDrive' ? 'Google Drive' : 'Local storage'}
                </p>
                <p className="text-xs text-ink-muted">
                  {data.googleDriveConnected
                    ? `Connected${data.folderId ? ` • folder ${data.folderId.slice(0, 8)}…` : ''}`
                    : 'Documents are stored on the server'}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Badge className={data.healthy ? 'bg-valid-bg text-valid-text' : 'bg-danger-bg text-danger-text'}>
                <CheckCircle2 className="mr-1 h-3 w-3" /> {data.healthy ? 'Healthy' : 'Attention'}
              </Badge>
            </div>
          </Card>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Stat icon={HardDrive} label="Storage used" value={formatBytes(data.usedBytes)} />
            <Stat icon={FileText} label="Documents" value={String(data.documentCount)} />
            <Stat icon={FolderKanban} label="Projects" value={String(data.projectCount)} />
          </div>

          <p className="text-xs text-ink-faint">
            Connect or disconnect Google Drive from <span className="font-medium">Settings → Storage</span>.
          </p>
        </>
      )}
    </div>
  );
}
