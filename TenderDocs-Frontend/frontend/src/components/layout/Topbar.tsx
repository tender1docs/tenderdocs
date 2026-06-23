import { useState } from 'react';
import { Menu, Search, Upload as UploadIcon, LogOut, RefreshCw } from 'lucide-react';
import { AnimatePresence, motion } from 'framer-motion';
import { Avatar, Button, IconButton } from '@/components/ui';
import { useMe } from '@/hooks';
import { useAuth } from '@/auth/AuthProvider';
import { can } from '@/lib/access';

const ROLE_LABEL: Record<string, string> = { approver: 'Approver', uploader: 'Uploader', viewer: 'Viewer' };

function UserMenu() {
  const { data: me } = useMe();
  const { logout, switchRole, role } = useAuth();
  const [open, setOpen] = useState(false);
  return (
    <div className="relative">
      <button onClick={() => setOpen((o) => !o)} className="rounded-full" aria-label="Account">
        <Avatar initials={me?.initials ?? '—'} />
      </button>
      <AnimatePresence>
        {open && (
          <>
            <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
            <motion.div initial={{ opacity: 0, scale: 0.96, y: -4 }} animate={{ opacity: 1, scale: 1, y: 0 }} exit={{ opacity: 0 }}
              className="absolute right-0 z-20 mt-2 w-56 overflow-hidden rounded-xl border border-line bg-white py-1 shadow-lift dark:border-[#222A31] dark:bg-[#12181D]">
              {me && (
                <div className="border-b border-line px-3 py-2 dark:border-[#222A31]">
                  <p className="truncate text-sm font-medium text-ink dark:text-slate-100">{me.name}</p>
                  <p className="truncate text-xs text-ink-muted">{me.email}</p>
                  {role && (
                    <span className="mt-1.5 inline-flex items-center rounded-full bg-brand-50 px-2 py-0.5 text-[11px] font-semibold text-brand-700 dark:bg-brand-900/40 dark:text-brand-200">
                      {ROLE_LABEL[role]}
                    </span>
                  )}
                </div>
              )}
              <button onClick={() => { setOpen(false); switchRole(); }}
                className="flex w-full items-center gap-2 px-3 py-2 text-sm text-ink-soft hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-[#161D23]">
                <RefreshCw className="h-4 w-4" /> Switch role
              </button>
              <button onClick={() => { setOpen(false); logout(); }}
                className="flex w-full items-center gap-2 px-3 py-2 text-sm text-ink-soft hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-[#161D23]">
                <LogOut className="h-4 w-4" /> Sign out
              </button>
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </div>
  );
}

export function Topbar({ onMenu, onSearch, onUpload }: {
  onMenu: () => void; onSearch: () => void; onUpload: () => void;
}) {
  const { role } = useAuth();
  const canUpload = !!role && can(role, 'upload');
  return (
    <header className="sticky top-0 z-30 flex h-[72px] items-center gap-3 border-b border-line bg-canvas/80 px-4 backdrop-blur dark:border-[#1A2127] dark:bg-[#0B0F12]/80 lg:px-8">
      <IconButton className="lg:hidden" onClick={onMenu} aria-label="Open menu"><Menu className="h-5 w-5" /></IconButton>

      <button
        onClick={onSearch}
        className="hidden h-10 max-w-md flex-1 items-center gap-2.5 rounded-xl border border-line bg-white px-3.5 text-sm text-ink-faint transition-colors hover:border-brand-200 dark:border-[#222A31] dark:bg-[#12181D] sm:flex">
        <Search className="h-4 w-4" />
        <span className="flex-1 text-left">Search everything…</span>
        <kbd className="rounded-md border border-line px-1.5 py-0.5 text-[11px] dark:border-[#222A31]">⌘K</kbd>
      </button>

      <div className="flex flex-1 items-center justify-end gap-3 sm:flex-none">
        <IconButton className="sm:hidden" onClick={onSearch} aria-label="Search"><Search className="h-5 w-5" /></IconButton>
        {canUpload && <Button onClick={onUpload}><UploadIcon className="h-4 w-4" /> Upload</Button>}
        <UserMenu />
      </div>
    </header>
  );
}
