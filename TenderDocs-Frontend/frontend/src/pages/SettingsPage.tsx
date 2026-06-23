import { motion } from 'framer-motion';
import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import {
  Sun, Moon, Check, Cloud, CloudOff, Server, ShieldCheck, Bell,
} from 'lucide-react';
import { useMe, useTheme, useToast } from '@/hooks';
import { Avatar, Button, Card, Input } from '@/components/ui';
import { apiClients } from '@/services';
import { cn } from '@/lib/utils';

function Toggle({ on, onChange }: { on: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      onClick={() => onChange(!on)}
      className={cn(
        'relative h-6 w-11 shrink-0 rounded-full transition-colors',
        on ? 'bg-brand-600' : 'bg-slate-300 dark:bg-[#2A333B]',
      )}
      role="switch"
      aria-checked={on}
    >
      <motion.span
        layout
        transition={{ type: 'spring', stiffness: 500, damping: 30 }}
        className={cn('absolute top-0.5 h-5 w-5 rounded-full bg-white shadow-card', on ? 'left-[22px]' : 'left-0.5')}
      />
    </button>
  );
}

function SectionCard({ title, desc, children }: { title: string; desc?: string; children: React.ReactNode }) {
  return (
    <Card className="p-5">
      <h2 className="text-sm font-semibold text-ink dark:text-slate-100">{title}</h2>
      {desc && <p className="mt-0.5 text-sm text-ink-muted">{desc}</p>}
      <div className="mt-4">{children}</div>
    </Card>
  );
}

export default function SettingsPage() {
  const { data: me } = useMe();
  const queryClient = useQueryClient();
  const [savingProfile, setSavingProfile] = useState(false);

  async function saveProfile() {
    setSavingProfile(true);
    try {
      await apiClients.AuthApi.updateProfile(name.trim());
      await queryClient.invalidateQueries({ queryKey: ['me'] });
      toast.push({ title: 'Profile saved', tone: 'success' });
    } catch {
      toast.push({ title: 'Could not save profile', tone: 'danger' });
    } finally {
      setSavingProfile(false);
    }
  }
  const { theme, setTheme } = useTheme();
  const toast = useToast();

  const [name, setName] = useState(me?.name ?? '');
  const [email, setEmail] = useState(me?.email ?? '');
  const [notif, setNotif] = useState({ expiry: true, project: true, weekly: false });

  // Real Google Drive / storage status from the backend.
  const [activeProvider, setActiveProvider] = useState<string>('Local');
  const [driveConnected, setDriveConnected] = useState(false);
  const [driveFolderId, setDriveFolderId] = useState<string | null>(null);
  const [connecting, setConnecting] = useState(false);

  useEffect(() => { if (me) { setName(me.name); setEmail(me.email); } }, [me]);

  async function refreshStatus() {
    try {
      const s = await apiClients.StorageApi.status();
      setActiveProvider(s.activeProvider);
      setDriveConnected(s.googleDriveConnected);
      setDriveFolderId(s.googleDriveFolderId);
    } catch {
      /* status unavailable — leave defaults */
    }
  }

  useEffect(() => {
    // Surface the result of the OAuth redirect (?drive=connected|error) then clean the URL.
    const params = new URLSearchParams(window.location.search);
    const driveResult = params.get('drive');
    if (driveResult === 'connected') toast.push({ title: 'Google Drive connected', tone: 'success' });
    else if (driveResult === 'error') toast.push({ title: 'Google Drive connection failed', tone: 'danger' });
    if (driveResult) window.history.replaceState({}, '', window.location.pathname);
    refreshStatus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function connectDrive() {
    setConnecting(true);
    try {
      const { url } = await apiClients.StorageApi.authorizeGoogleDrive();
      window.location.href = url; // hand off to Google consent screen
    } catch {
      setConnecting(false);
      toast.push({ title: 'Google Drive is not configured on the server', tone: 'danger' });
    }
  }

  async function disconnectDrive() {
    try {
      await apiClients.StorageApi.disconnectGoogleDrive();
      toast.push({ title: 'Google Drive disconnected', tone: 'default' });
    } catch {
      toast.push({ title: 'Could not disconnect', tone: 'danger' });
    } finally {
      refreshStatus();
    }
  }

  return (
    <div className="max-w-3xl">
      <div className="mb-6">
        <h1 className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">Settings</h1>
        <p className="mt-1 text-sm text-ink-muted">Manage your profile, appearance, and integrations.</p>
      </div>

      <div className="space-y-5">
        {/* Profile */}
        <SectionCard title="Profile" desc="This information appears across your workspace.">
          <div className="flex items-center gap-4">
            <Avatar initials={me?.initials ?? '—'} className="h-14 w-14 text-base" />
            <div className="grid flex-1 grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1.5 block text-xs font-medium text-ink-muted">Name</label>
                <Input value={name} onChange={(e) => setName(e.target.value)} />
              </div>
              <div>
                <label className="mb-1.5 block text-xs font-medium text-ink-muted">Email</label>
                <Input value={email} onChange={(e) => setEmail(e.target.value)} type="email" />
              </div>
            </div>
          </div>
          <div className="mt-4 flex justify-end">
            <Button size="sm" loading={savingProfile} onClick={saveProfile}>Save changes</Button>
          </div>
        </SectionCard>

        {/* Appearance */}
        <SectionCard title="Appearance" desc="Choose how TenderDocs looks on this device.">
          <div className="grid grid-cols-2 gap-3">
            {([['light', Sun, 'Light'], ['dark', Moon, 'Dark']] as const).map(([val, Icon, label]) => (
              <button
                key={val}
                onClick={() => setTheme(val)}
                className={cn(
                  'flex items-center gap-3 rounded-xl border p-3.5 text-left transition-all',
                  theme === val ? 'border-brand-500 ring-1 ring-brand-200 dark:ring-brand-900/40' : 'border-line hover:border-brand-300 dark:border-[#222A31]',
                )}
              >
                <span className={cn('flex h-9 w-9 items-center justify-center rounded-lg', theme === val ? 'bg-brand-600 text-white' : 'bg-slate-100 text-ink-soft dark:bg-[#1B232A]')}>
                  <Icon className="h-4 w-4" />
                </span>
                <span className="flex-1 text-sm font-medium text-ink dark:text-slate-100">{label}</span>
                {theme === val && <Check className="h-4 w-4 text-brand-600" />}
              </button>
            ))}
          </div>
        </SectionCard>

        {/* Notifications */}
        <SectionCard title="Notifications" desc="Decide what TenderDocs should alert you about.">
          <div className="space-y-1">
            {([
              ['expiry', 'Expiry alerts', 'Warn me before a document expires'],
              ['project', 'Project updates', 'Package generation and document changes'],
              ['weekly', 'Weekly digest', 'A summary of activity every Monday'],
            ] as const).map(([key, label, desc]) => (
              <div key={key} className="flex items-center justify-between gap-4 py-2.5">
                <div className="flex items-start gap-3">
                  <span className="mt-0.5 flex h-8 w-8 items-center justify-center rounded-lg bg-slate-100 text-ink-soft dark:bg-[#1B232A]">
                    <Bell className="h-4 w-4" />
                  </span>
                  <div>
                    <p className="text-sm font-medium text-ink dark:text-slate-100">{label}</p>
                    <p className="text-xs text-ink-muted">{desc}</p>
                  </div>
                </div>
                <Toggle on={notif[key]} onChange={(v) => setNotif((n) => ({ ...n, [key]: v }))} />
              </div>
            ))}
          </div>
        </SectionCard>

        {/* Storage / Google Drive */}
        <SectionCard title="Storage providers" desc="Connect Google Drive to store tender documents in your own Drive folder.">
          <div className="flex items-center gap-3.5 rounded-xl border border-line p-4 dark:border-[#222A31]">
            <span className={cn(
              'flex h-11 w-11 items-center justify-center rounded-xl',
              driveConnected ? 'bg-valid-bg text-valid-text' : 'bg-slate-100 text-ink-soft dark:bg-[#1B232A]',
            )}>
              {driveConnected ? <Cloud className="h-5 w-5" /> : <CloudOff className="h-5 w-5" />}
            </span>
            <div className="flex-1">
              <p className="text-sm font-semibold text-ink dark:text-slate-100">Google Drive</p>
              <p className="text-xs text-ink-muted">
                {driveConnected
                  ? `Connected • documents sync to Drive${driveFolderId ? ` (folder ${driveFolderId.slice(0, 8)}…)` : ''}`
                  : 'Not connected — uploads are stored locally'}
              </p>
            </div>
            {driveConnected ? (
              <Button variant="secondary" size="sm" onClick={disconnectDrive}>Disconnect</Button>
            ) : (
              <Button size="sm" onClick={connectDrive} loading={connecting}>
                {connecting ? 'Redirecting…' : 'Connect'}
              </Button>
            )}
          </div>
        </SectionCard>

        {/* Backend status */}
        <SectionCard title="System" desc="Connection to the TenderDocs API.">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="flex items-center gap-3 rounded-xl border border-line p-3.5 dark:border-[#222A31]">
              <Server className="h-5 w-5 text-ink-muted" />
              <div className="flex-1">
                <p className="text-sm font-medium text-ink dark:text-slate-100">Active storage</p>
                <p className="text-xs text-ink-muted">{activeProvider === 'GoogleDrive' ? 'Google Drive' : 'Local disk'}</p>
              </div>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-valid-bg px-2 py-1 text-xs font-medium text-valid-text">
                <Check className="h-3 w-3" /> Live
              </span>
            </div>
            <div className="flex items-center gap-3 rounded-xl border border-line p-3.5 dark:border-[#222A31]">
              <ShieldCheck className="h-5 w-5 text-valid-text" />
              <div className="flex-1">
                <p className="text-sm font-medium text-ink dark:text-slate-100">API</p>
                <p className="text-xs text-ink-muted">ASP.NET Core + PostgreSQL</p>
              </div>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-valid-bg px-2 py-1 text-xs font-medium text-valid-text">
                <Check className="h-3 w-3" /> Connected
              </span>
            </div>
          </div>
        </SectionCard>
      </div>
    </div>
  );
}
