import { motion } from 'framer-motion';
import { useState } from 'react';
import { UserPlus, Mail, Shield } from 'lucide-react';
import type { TeamMember } from '@/types';
import { useTeam } from '@/hooks';
import { useToast } from '@/hooks';
import { Avatar, Badge, Button, Card, Input, Modal, Select, Skeleton } from '@/components/ui';
import { cn } from '@/lib/utils';

const roleCls: Record<TeamMember['role'], string> = {
  Owner: 'bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200',
  Admin: 'bg-violet-50 text-violet-700 dark:bg-violet-950/40 dark:text-violet-300',
  Editor: 'bg-sky-50 text-sky-700 dark:bg-sky-950/40 dark:text-sky-300',
  Viewer: 'bg-slate-100 text-ink-soft dark:bg-[#1B232A] dark:text-slate-300',
};

export default function TeamPage() {
  const { data, isLoading } = useTeam();
  const toast = useToast();
  const [invite, setInvite] = useState(false);
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<TeamMember['role']>('Editor');

  const sendInvite = () => {
    if (!email.trim()) return;
    toast.push({ title: `Invitation sent to ${email}`, tone: 'success' });
    setEmail('');
    setInvite(false);
  };

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">Team</h1>
          <p className="mt-1 text-sm text-ink-muted">Manage who can access and edit your tender documents.</p>
        </div>
        <Button size="sm" onClick={() => setInvite(true)}>
          <UserPlus className="h-4 w-4" /> Invite member
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2.5">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-16" />)}
        </div>
      ) : (
        <Card className="divide-y divide-line p-0 dark:divide-[#1F262C]">
          {data?.map((m, i) => (
            <motion.div
              key={m.id}
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.05, duration: 0.3 }}
              className="flex items-center gap-4 p-4"
            >
              <Avatar initials={m.initials} className="h-10 w-10" />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <p className="truncate text-sm font-semibold text-ink dark:text-slate-100">{m.name}</p>
                  {m.status === 'invited' && (
                    <Badge className="bg-warn-bg text-warn-text">Invited</Badge>
                  )}
                </div>
                <p className="flex items-center gap-1 text-xs text-ink-muted">
                  <Mail className="h-3 w-3" /> {m.email}
                </p>
              </div>
              <span className={cn('inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-semibold', roleCls[m.role])}>
                <Shield className="h-3 w-3" /> {m.role}
              </span>
            </motion.div>
          ))}
        </Card>
      )}

      <Modal open={invite} onClose={() => setInvite(false)} title="Invite a team member">
        <div className="space-y-4">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-ink-soft dark:text-slate-300">Email address</label>
            <Input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@company.com" type="email" />
          </div>
          <div>
            <label className="mb-1.5 block text-sm font-medium text-ink-soft dark:text-slate-300">Role</label>
            <Select value={role} onChange={(e) => setRole(e.target.value as TeamMember['role'])}>
              <option value="Admin">Admin — manage everything</option>
              <option value="Editor">Editor — upload &amp; organize</option>
              <option value="Viewer">Viewer — read only</option>
            </Select>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={() => setInvite(false)}>Cancel</Button>
            <Button onClick={sendInvite} disabled={!email.trim()}>Send invite</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
