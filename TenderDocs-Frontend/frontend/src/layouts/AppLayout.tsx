import { useEffect, useState } from 'react';
import { Outlet, Navigate, useLocation } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';
import { Sidebar } from '@/components/layout/Sidebar';
import { Topbar } from '@/components/layout/Topbar';
import { GlobalSearch } from '@/components/layout/GlobalSearch';
import { UploadDialog } from '@/components/layout/UploadDialog';
import { useMediaQuery } from '@/hooks';
import { useAuth } from '@/auth/AuthProvider';
import { canVisit } from '@/lib/access';
import { DocumentDrawerProvider } from '@/features/documents/DocumentDrawer';

/** Redirects to the dashboard when the current role may not visit the requested page. */
function GuardedOutlet() {
  const { role } = useAuth();
  const { pathname } = useLocation();
  if (role && !canVisit(role, pathname)) return <Navigate to="/dashboard" replace />;
  return <Outlet />;
}

export function AppLayout() {
  const [drawer, setDrawer] = useState(false);
  const [search, setSearch] = useState(false);
  const [upload, setUpload] = useState(false);
  const isDesktop = useMediaQuery('(min-width: 1024px)');

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); setSearch(true); }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  return (
    <DocumentDrawerProvider>
    <div className="flex h-screen overflow-hidden bg-canvas dark:bg-[#0B0F12]">
      {isDesktop && <Sidebar />}

      <AnimatePresence>
        {!isDesktop && drawer && (
          <>
            <motion.div className="fixed inset-0 z-40 bg-slate-900/40 backdrop-blur-sm"
              initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} onClick={() => setDrawer(false)} />
            <motion.div className="fixed inset-y-0 left-0 z-50"
              initial={{ x: -280 }} animate={{ x: 0 }} exit={{ x: -280 }}
              transition={{ type: 'spring', stiffness: 380, damping: 34 }}>
              <Sidebar onNavigate={() => setDrawer(false)} />
            </motion.div>
          </>
        )}
      </AnimatePresence>

      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar onMenu={() => setDrawer(true)} onSearch={() => setSearch(true)} onUpload={() => setUpload(true)} />
        <main className="flex-1 overflow-y-auto px-4 py-6 lg:px-8 lg:py-8">
          <div className="mx-auto max-w-[1180px]">
            <GuardedOutlet />
          </div>
        </main>
      </div>

      <GlobalSearch open={search} onClose={() => setSearch(false)} />
      <UploadDialog open={upload} onClose={() => setUpload(false)} />
    </div>
    </DocumentDrawerProvider>
  );
}
