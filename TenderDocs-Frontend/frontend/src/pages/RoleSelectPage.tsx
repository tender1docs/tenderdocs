import { useState } from 'react';
import { motion } from 'framer-motion';
import { Upload, ShieldCheck, Eye, Check, LogOut, type LucideIcon } from 'lucide-react';
import { Logo } from '@/components/layout/Logo';
import { useAuth } from '@/auth/AuthProvider';
import { cn } from '@/lib/utils';
import type { Role } from '@/types';

const ROLES: { value: Role; label: string; icon: LucideIcon; tagline: string; access: string[] }[] = [
  {
    value: 'uploader', label: 'Uploader', icon: Upload,
    tagline: 'Add documents and build out projects.',
    access: ['Dashboard, Documents & Projects', 'Upload and add documents', 'No approvals or settings'],
  },
  {
    value: 'approver', label: 'Approver', icon: ShieldCheck,
    tagline: 'Full access — review and approve everything.',
    access: ['Every page, including Organize & Settings', 'Approve or reject documents', 'Manage categories & structure'],
  },
  {
    value: 'viewer', label: 'Viewer', icon: Eye,
    tagline: 'Browse documents and projects, read-only.',
    access: ['Dashboard, Documents, Projects & Organize', 'View everything', 'No uploading or editing'],
  },
];

export default function RoleSelectPage() {
  const { role, selectRole, logout } = useAuth();
  const [busy, setBusy] = useState<Role | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function pick(next: Role) {
    setError(null);
    setBusy(next);
    try {
      await selectRole(next);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not set role');
      setBusy(null);
    }
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-canvas px-6 py-12 dark:bg-[#0B0F12]">
      <div className="w-full max-w-4xl">
        <div className="flex items-center justify-between">
          <Logo />
          <button onClick={logout} className="inline-flex items-center gap-1.5 text-sm font-medium text-ink-muted hover:text-brand-600">
            <LogOut className="h-4 w-4" /> Sign out
          </button>
        </div>

        <div className="mt-10 text-center">
          <h1 className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">How do you want to use the app?</h1>
          <p className="mt-1.5 text-sm text-ink-muted">Pick a role — you can switch anytime from the account menu.</p>
        </div>

        <div className="mt-8 grid grid-cols-1 gap-5 sm:grid-cols-3">
          {ROLES.map((r, i) => {
            const Icon = r.icon;
            const current = role === r.value;
            return (
              <motion.button
                key={r.value}
                onClick={() => pick(r.value)}
                disabled={busy !== null}
                initial={{ opacity: 0, y: 16 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3, delay: i * 0.06 }}
                whileHover={{ y: -4 }}
                className={cn(
                  'surface group relative flex flex-col items-start p-6 text-left transition-shadow hover:shadow-lift disabled:opacity-60',
                  current ? 'ring-2 ring-brand-500' : 'ring-1 ring-transparent',
                )}
              >
                {current && (
                  <span className="absolute right-4 top-4 inline-flex items-center gap-1 rounded-full bg-brand-50 px-2 py-0.5 text-[11px] font-semibold text-brand-700 dark:bg-brand-900/40 dark:text-brand-200">
                    <Check className="h-3 w-3" /> Current
                  </span>
                )}
                <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-200">
                  <Icon className="h-6 w-6" />
                </span>
                <h2 className="mt-4 text-lg font-semibold text-ink dark:text-slate-100">{r.label}</h2>
                <p className="mt-1 text-sm text-ink-muted">{r.tagline}</p>
                <ul className="mt-4 space-y-1.5">
                  {r.access.map((a) => (
                    <li key={a} className="flex items-start gap-2 text-xs text-ink-soft dark:text-slate-300">
                      <Check className="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-500" /> {a}
                    </li>
                  ))}
                </ul>
                <span className="mt-5 inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 group-hover:gap-2.5">
                  {busy === r.value ? 'Entering…' : current ? 'Continue' : 'Use this role'}
                </span>
              </motion.button>
            );
          })}
        </div>

        {error && <p className="mt-6 text-center text-sm text-danger-text">{error}</p>}
      </div>
    </div>
  );
}
