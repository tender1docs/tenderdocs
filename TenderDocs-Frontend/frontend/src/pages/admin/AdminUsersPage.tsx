import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { UserPlus, Mail, Search, ShieldCheck, KeyRound, Trash2 } from 'lucide-react';
import { Card, Button, IconButton, Modal, Input, Select, Badge, Avatar, Skeleton, EmptyState } from '@/components/ui';
import { useConfirm } from '@/components/ui/confirm';
import { useToast, useMe } from '@/hooks';
import { UsersApi } from '@/services/api';
import type { TeamMemberDto } from '@/services/api';
import { ApiError } from '@/config/api';
import { cn } from '@/lib/utils';

const ROLES = [
  { value: 'Admin', label: 'Admin — full access' },
  { value: 'Uploader', label: 'Uploader — document work' },
  { value: 'Approver', label: 'Approver — approvals only' },
  { value: 'Viewer', label: 'Viewer — read only' },
];

const ROLE_TONE: Record<string, string> = {
  Admin: 'bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200',
  Approver: 'bg-warn-bg text-warn-text',
  Uploader: 'bg-valid-bg text-valid-text',
  Viewer: 'bg-slate-100 text-ink-soft dark:bg-[#1B232A] dark:text-slate-300',
};

const QK = ['admin', 'users'];
const errMsg = (e: unknown) => (e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Something went wrong');

export default function AdminUsersPage() {
  const qc = useQueryClient();
  const { push } = useToast();
  const confirm = useConfirm();
  const { data: me } = useMe();

  const { data: users, isLoading } = useQuery({ queryKey: QK, queryFn: () => UsersApi.list() });

  const [query, setQuery] = useState('');
  const [addOpen, setAddOpen] = useState(false);
  const [resetFor, setResetFor] = useState<TeamMemberDto | null>(null);

  const invalidate = () => qc.invalidateQueries({ queryKey: QK });

  const createUser = useMutation({
    mutationFn: (input: { email: string; fullName: string; role: string; password?: string }) =>
      UsersApi.create(input),
    onSuccess: (u) => { invalidate(); setAddOpen(false); push({ title: `Added ${u.fullName}`, tone: 'success' }); },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  const updateRole = useMutation({
    mutationFn: ({ id, role }: { id: string; role: string }) => UsersApi.updateRole(id, role),
    onSuccess: (u) => { invalidate(); push({ title: `${u.fullName} is now ${u.role}`, tone: 'success' }); },
    onError: (e) => { invalidate(); push({ title: errMsg(e), tone: 'danger' }); },
  });

  const setActive = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => UsersApi.setActive(id, isActive),
    onSuccess: (u) => { invalidate(); push({ title: `${u.fullName} ${u.isActive ? 'activated' : 'deactivated'}`, tone: 'success' }); },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  const resetPassword = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) => UsersApi.resetPassword(id, password),
    onSuccess: () => { setResetFor(null); push({ title: 'Password reset', tone: 'success' }); },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  const deleteUser = useMutation({
    mutationFn: (id: string) => UsersApi.remove(id),
    onSuccess: () => { invalidate(); push({ title: 'User deleted', tone: 'success' }); },
    onError: (e) => push({ title: errMsg(e), tone: 'danger' }),
  });

  async function removeUser(u: TeamMemberDto) {
    const ok = await confirm({
      title: `Delete ${u.fullName}?`,
      message: 'This removes their access to the workspace.',
      confirmText: 'Delete', tone: 'danger',
    });
    if (ok) deleteUser.mutate(u.id);
  }

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const list = users ?? [];
    if (!q) return list;
    return list.filter((u) => u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q));
  }, [users, query]);

  async function toggleActive(u: TeamMemberDto) {
    if (u.isActive) {
      const ok = await confirm({
        title: `Deactivate ${u.fullName}?`,
        message: 'They will be signed out and unable to sign in until reactivated.',
        confirmText: 'Deactivate', tone: 'danger',
      });
      if (!ok) return;
    }
    setActive.mutate({ id: u.id, isActive: !u.isActive });
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative w-full sm:max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-muted" />
          <Input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search users…" className="pl-9" />
        </div>
        <Button onClick={() => setAddOpen(true)}><UserPlus className="h-4 w-4" /> Add user</Button>
      </div>

      {isLoading ? (
        <Card className="divide-y divide-line p-0 dark:divide-[#222A31]">
          {Array.from({ length: 4 }).map((_, i) => <div key={i} className="p-4"><Skeleton className="h-10" /></div>)}
        </Card>
      ) : filtered.length === 0 ? (
        <EmptyState icon={<ShieldCheck className="h-6 w-6" />} title="No users found"
          hint={query ? 'Try a different search.' : 'Add your first user to get started.'} />
      ) : (
        <Card className="divide-y divide-line p-0 dark:divide-[#222A31]">
          {filtered.map((u, i) => {
            const isSelf = me?.email?.toLowerCase() === u.email.toLowerCase();
            const rolePending = updateRole.isPending && updateRole.variables?.id === u.id;
            const activePending = setActive.isPending && setActive.variables?.id === u.id;
            return (
              <motion.div key={u.id}
                initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.03 }}
                className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center">
                <Avatar initials={u.initials} className="h-10 w-10" />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <p className="truncate text-sm font-semibold text-ink dark:text-slate-100">{u.fullName}</p>
                    {isSelf && <Badge className="bg-slate-100 text-ink-soft dark:bg-[#1B232A] dark:text-slate-300">You</Badge>}
                    {!u.isActive && <Badge className="bg-danger-bg text-danger-text">Disabled</Badge>}
                  </div>
                  <p className="flex items-center gap-1 text-xs text-ink-muted"><Mail className="h-3 w-3" /> {u.email}</p>
                </div>

                <Badge className={cn('hidden sm:inline-flex', ROLE_TONE[u.role] ?? ROLE_TONE.Viewer)}>{u.role}</Badge>

                <div className="flex items-center gap-2">
                  <Select
                    aria-label={`Role for ${u.fullName}`}
                    value={u.role}
                    disabled={rolePending}
                    onChange={(e) => { if (e.target.value !== u.role) updateRole.mutate({ id: u.id, role: e.target.value }); }}
                    className="w-36"
                  >
                    {ROLES.map((r) => <option key={r.value} value={r.value}>{r.value}</option>)}
                  </Select>
                  <Button
                    variant={u.isActive ? 'secondary' : 'primary'}
                    size="sm"
                    loading={activePending}
                    disabled={isSelf}
                    title={isSelf ? "You can't deactivate your own account" : undefined}
                    onClick={() => toggleActive(u)}
                  >
                    {u.isActive ? 'Deactivate' : 'Activate'}
                  </Button>
                  <IconButton aria-label="Reset password" title="Reset password" onClick={() => setResetFor(u)}>
                    <KeyRound className="h-4 w-4" />
                  </IconButton>
                  <IconButton aria-label="Delete user" disabled={isSelf}
                    title={isSelf ? "You can't delete your own account" : 'Delete user'} onClick={() => removeUser(u)}>
                    <Trash2 className="h-4 w-4" />
                  </IconButton>
                </div>
              </motion.div>
            );
          })}
        </Card>
      )}

      <AddUserModal open={addOpen} onClose={() => setAddOpen(false)}
        onSubmit={(v) => createUser.mutate(v)} submitting={createUser.isPending} />

      <Modal open={!!resetFor} onClose={() => setResetFor(null)} title={`Reset password — ${resetFor?.fullName ?? ''}`}>
        <ResetPasswordForm submitting={resetPassword.isPending} onCancel={() => setResetFor(null)}
          onSubmit={(pw) => resetFor && resetPassword.mutate({ id: resetFor.id, password: pw })} />
      </Modal>
    </div>
  );
}

function ResetPasswordForm({ onSubmit, onCancel, submitting }: {
  onSubmit: (pw: string) => void; onCancel: () => void; submitting: boolean;
}) {
  const [pw, setPw] = useState('');
  const valid = pw.length >= 8;
  return (
    <form onSubmit={(e) => { e.preventDefault(); if (valid) onSubmit(pw); }} className="space-y-4">
      <div>
        <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">New password</label>
        <Input type="password" value={pw} onChange={(e) => setPw(e.target.value)} placeholder="At least 8 characters" autoFocus />
        {pw !== '' && !valid && <p className="mt-1 text-xs text-danger-text">Password must be at least 8 characters.</p>}
      </div>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
        <Button type="submit" loading={submitting} disabled={!valid}>Reset password</Button>
      </div>
    </form>
  );
}

function AddUserModal({ open, onClose, onSubmit, submitting }: {
  open: boolean; onClose: () => void;
  onSubmit: (v: { email: string; fullName: string; role: string; password?: string }) => void;
  submitting: boolean;
}) {
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [role, setRole] = useState('Viewer');
  const [password, setPassword] = useState('');

  // Reset when reopened.
  const reset = () => { setEmail(''); setFullName(''); setRole('Viewer'); setPassword(''); };

  const valid = email.trim() !== '' && fullName.trim() !== '' && (password === '' || password.length >= 8);

  return (
    <Modal open={open} onClose={() => { onClose(); reset(); }} title="Add user">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (!valid) return;
          onSubmit({ email: email.trim(), fullName: fullName.trim(), role, password: password || undefined });
        }}
        className="space-y-4"
      >
        <div>
          <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">Email (Google account)</label>
          <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="person@gmail.com" required autoFocus />
        </div>
        <div>
          <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">Full name</label>
          <Input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Jane Doe" required />
        </div>
        <div>
          <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">Role</label>
          <Select value={role} onChange={(e) => setRole(e.target.value)}>
            {ROLES.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
          </Select>
        </div>
        <div>
          <label className="mb-1.5 block text-sm font-medium text-ink dark:text-slate-200">Password <span className="text-ink-faint">(optional)</span></label>
          <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Leave blank for Google-only sign-in" />
          {password !== '' && password.length < 8 && (
            <p className="mt-1 text-xs text-danger-text">Password must be at least 8 characters.</p>
          )}
        </div>
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={() => { onClose(); reset(); }}>Cancel</Button>
          <Button type="submit" loading={submitting} disabled={!valid}>Add user</Button>
        </div>
      </form>
    </Modal>
  );
}
