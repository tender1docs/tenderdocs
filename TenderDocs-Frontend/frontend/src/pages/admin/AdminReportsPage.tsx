import { useState } from 'react';
import { Download, Users, HardDrive, FolderKanban, Upload, CheckSquare, ScrollText, type LucideIcon } from 'lucide-react';
import { Card, Button } from '@/components/ui';
import { useToast } from '@/hooks';
import { AdminApi } from '@/services/api';
import { saveBlob } from '@/services';

const REPORTS: { type: string; title: string; desc: string; icon: LucideIcon }[] = [
  { type: 'users', title: 'User Activity', desc: 'All users — role, status and last login.', icon: Users },
  { type: 'storage', title: 'Storage', desc: 'Storage usage summary.', icon: HardDrive },
  { type: 'projects', title: 'Projects', desc: 'Projects and their document counts.', icon: FolderKanban },
  { type: 'uploads', title: 'Uploads', desc: 'Every uploaded document with status.', icon: Upload },
  { type: 'approvals', title: 'Approvals', desc: 'Approval decisions and reviewers.', icon: CheckSquare },
  { type: 'audit', title: 'Audit Logs', desc: 'Full audit trail (recent 5,000 events).', icon: ScrollText },
];

export default function AdminReportsPage() {
  const { push } = useToast();
  const [busy, setBusy] = useState<string | null>(null);

  async function download(type: string) {
    setBusy(type);
    try {
      saveBlob(await AdminApi.report(type), `${type}-report.csv`);
      push({ title: 'Report downloaded', tone: 'success' });
    } catch (e) {
      push({ title: e instanceof Error ? e.message : 'Download failed', tone: 'danger' });
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-ink dark:text-slate-100">Reports</h2>
        <p className="text-sm text-ink-muted">Export workspace data as CSV.</p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {REPORTS.map(({ type, title, desc, icon: Icon }) => (
          <Card key={type} className="flex items-center gap-4 p-5">
            <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-300">
              <Icon className="h-5 w-5" />
            </span>
            <div className="min-w-0 flex-1">
              <p className="font-semibold text-ink dark:text-slate-100">{title}</p>
              <p className="truncate text-sm text-ink-muted">{desc}</p>
            </div>
            <Button variant="secondary" size="sm" loading={busy === type} onClick={() => download(type)}>
              <Download className="h-4 w-4" /> CSV
            </Button>
          </Card>
        ))}
      </div>
    </div>
  );
}
