import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Users, FolderKanban, FileText, Clock, ArrowRight, type LucideIcon } from 'lucide-react';
import { Card, Avatar, Badge, Skeleton } from '@/components/ui';
import { useDocuments, useProjects } from '@/hooks';
import { UsersApi } from '@/services/api';

function Stat({ icon: Icon, label, value, loading }: {
  icon: LucideIcon; label: string; value: number | string; loading?: boolean;
}) {
  return (
    <Card className="flex items-center gap-4 p-5">
      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-300">
        <Icon className="h-5 w-5" />
      </span>
      <div className="min-w-0">
        <p className="text-sm text-ink-muted">{label}</p>
        {loading
          ? <Skeleton className="mt-1 h-7 w-12" />
          : <p className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">{value}</p>}
      </div>
    </Card>
  );
}

const ROLE_TONE: Record<string, string> = {
  Admin: 'bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200',
  Approver: 'bg-warn-bg text-warn-text',
  Uploader: 'bg-valid-bg text-valid-text',
  Viewer: 'bg-slate-100 text-ink-soft dark:bg-[#1B232A] dark:text-slate-300',
};

export default function AdminDashboardPage() {
  const { data: users, isLoading: usersLoading } = useQuery({
    queryKey: ['admin', 'users'], queryFn: () => UsersApi.list(),
  });
  const { data: projects = [], isLoading: projectsLoading } = useProjects();
  const { data: documents = [], isLoading: docsLoading } = useDocuments();

  const pending = documents.filter((d) => d.approval === 'pending').length;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Stat icon={Users} label="Users" value={users?.length ?? 0} loading={usersLoading} />
        <Stat icon={FolderKanban} label="Projects" value={projects.length} loading={projectsLoading} />
        <Stat icon={FileText} label="Documents" value={documents.length} loading={docsLoading} />
        <Stat icon={Clock} label="Pending approvals" value={pending} loading={docsLoading} />
      </div>

      <Card className="p-0">
        <div className="flex items-center justify-between border-b border-line px-5 py-4 dark:border-[#222A31]">
          <h2 className="font-semibold text-ink dark:text-slate-100">Users</h2>
          <Link to="/admin/users" className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:underline">
            Manage <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
        {usersLoading ? (
          <div className="space-y-2 p-5">
            {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-12" />)}
          </div>
        ) : users && users.length > 0 ? (
          <div className="divide-y divide-line dark:divide-[#222A31]">
            {users.slice(0, 5).map((u) => (
              <div key={u.id} className="flex items-center gap-3 px-5 py-3">
                <Avatar initials={u.initials} className="h-9 w-9" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-ink dark:text-slate-100">{u.fullName}</p>
                  <p className="truncate text-xs text-ink-muted">{u.email}</p>
                </div>
                <Badge className={ROLE_TONE[u.role] ?? ROLE_TONE.Viewer}>{u.role}</Badge>
                {!u.isActive && <Badge className="bg-danger-bg text-danger-text">Disabled</Badge>}
              </div>
            ))}
          </div>
        ) : (
          <p className="px-5 py-8 text-center text-sm text-ink-muted">No users yet.</p>
        )}
      </Card>
    </div>
  );
}
