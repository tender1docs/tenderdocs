import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { FolderKanban, Save, Check } from 'lucide-react';
import { Card, Button, Select, Skeleton, EmptyState } from '@/components/ui';
import { useToast, useProjects } from '@/hooks';
import { AdminApi, UsersApi } from '@/services/api';
import { ApiError } from '@/config/api';
import { cn } from '@/lib/utils';

const errMsg = (e: unknown) => (e instanceof ApiError || e instanceof Error ? e.message : 'Something went wrong');

export default function AdminProjectAccessPage() {
  const qc = useQueryClient();
  const { push } = useToast();
  const { data: users } = useQuery({ queryKey: ['admin', 'users'], queryFn: () => UsersApi.list() });
  const { data: projects = [] } = useProjects();

  const [userId, setUserId] = useState('');
  const selectedUser = users?.find((u) => u.id === userId);
  const isAdminUser = selectedUser?.role === 'Admin';

  const { data: assigned, isLoading: loadingAssigned } = useQuery({
    queryKey: ['admin', 'userProjects', userId],
    queryFn: () => AdminApi.userProjects(userId),
    enabled: !!userId && !isAdminUser,
  });

  const [selected, setSelected] = useState<Set<string>>(new Set());
  useEffect(() => { setSelected(new Set(assigned ?? [])); }, [assigned, userId]);

  const save = useMutation({
    mutationFn: () => AdminApi.setUserProjects(userId, [...selected]),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'userProjects', userId] });
      push({ title: 'Project access updated', tone: 'success' });
    },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  const toggle = (id: string) => setSelected((s) => {
    const n = new Set(s); n.has(id) ? n.delete(id) : n.add(id); return n;
  });

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-ink dark:text-slate-100">Project access</h2>
        <p className="text-sm text-ink-muted">Choose which projects a user can see. Non-admins only see projects assigned here.</p>
      </div>

      <div className="max-w-sm">
        <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">User</label>
        <Select value={userId} onChange={(e) => setUserId(e.target.value)}>
          <option value="">Select a user…</option>
          {(users ?? []).map((u) => <option key={u.id} value={u.id}>{u.fullName} ({u.role})</option>)}
        </Select>
      </div>

      {!userId ? (
        <EmptyState icon={<FolderKanban className="h-6 w-6" />} title="Select a user"
          hint="Pick a user above to manage which projects they can access." />
      ) : isAdminUser ? (
        <Card className="p-6 text-sm text-ink-soft dark:text-slate-300">
          <span className="font-medium">{selectedUser?.fullName}</span> is an Admin and can access every project.
        </Card>
      ) : projects.length === 0 ? (
        <EmptyState icon={<FolderKanban className="h-6 w-6" />} title="No projects yet"
          hint="Create projects first, then assign access." />
      ) : loadingAssigned ? (
        <Skeleton className="h-64" />
      ) : (
        <>
          <Card className="divide-y divide-line p-0 dark:divide-[#222A31]">
            {projects.map((p) => {
              const on = selected.has(p.id);
              return (
                <button type="button" key={p.id} onClick={() => toggle(p.id)}
                  className="flex w-full items-center gap-3 px-4 py-3 text-left transition-colors hover:bg-slate-50 dark:hover:bg-[#161D23]">
                  <span className={cn('flex h-6 w-6 shrink-0 items-center justify-center rounded-md border',
                    on ? 'border-brand-600 bg-brand-600 text-white' : 'border-line dark:border-[#2A333B]')}>
                    {on && <Check className="h-4 w-4" />}
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-ink dark:text-slate-100">{p.name}</p>
                    {p.description && <p className="truncate text-xs text-ink-muted">{p.description}</p>}
                  </div>
                </button>
              );
            })}
          </Card>
          <div className="flex items-center justify-between">
            <span className="text-sm text-ink-muted">{selected.size} of {projects.length} selected</span>
            <Button loading={save.isPending} onClick={() => save.mutate()}><Save className="h-4 w-4" /> Save access</Button>
          </div>
        </>
      )}
    </div>
  );
}
