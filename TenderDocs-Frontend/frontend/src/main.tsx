import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { ToastProvider } from '@/components/ui';
import { ConfirmProvider } from '@/components/ui/confirm';
import { AuthProvider, useAuth } from '@/auth/AuthProvider';
import { lazy, Suspense, useEffect } from 'react';
import { router } from '@/routes';
import './styles.css';

// After a redeploy, a tab still running the previous build holds stale chunk filenames;
// navigating to a lazy route then fails to fetch the (now-removed) chunk. Reload once to
// pick up the fresh index.html + chunks. The short time-guard prevents a reload loop.
window.addEventListener('vite:preloadError', () => {
  const now = Date.now();
  const last = Number(sessionStorage.getItem('chunkReloadedAt') || '0');
  if (now - last < 15_000) return;
  sessionStorage.setItem('chunkReloadedAt', String(now));
  window.location.reload();
});

const LoginPage = lazy(() => import('@/pages/LoginPage'));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, refetchOnWindowFocus: false, retry: 1 },
  },
});

const ScreenFallback = () => <div className="min-h-screen bg-canvas dark:bg-[#0B0F12]" />;

/** Shown if a signed-in account has no permissions yet (an admin must grant access). */
function AccessPending() {
  const { logout } = useAuth();
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-canvas px-6 text-center dark:bg-[#0B0F12]">
      <h1 className="text-xl font-semibold text-ink dark:text-slate-100">Access pending</h1>
      <p className="max-w-sm text-sm text-ink-muted">
        Your account doesn’t have access yet. Ask your administrator to assign you a role.
      </p>
      <button onClick={logout} className="text-sm font-medium text-brand-600 hover:underline">Sign out</button>
    </div>
  );
}

/** Login → app. Access is controlled by the API; there is no role picker. */
function Gate() {
  const { authed, ready, permissions } = useAuth();
  // Drop any cached data when the session ends so the next sign-in starts clean.
  useEffect(() => { if (!authed) queryClient.clear(); }, [authed]);
  if (!authed) {
    return <Suspense fallback={<ScreenFallback />}><LoginPage /></Suspense>;
  }
  if (!ready) {
    return <ScreenFallback />;   // briefly loading the session's role + permissions
  }
  if (permissions.length === 0) {
    return <AccessPending />;
  }
  return <RouterProvider router={router} />;
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <ConfirmProvider>
          <AuthProvider>
            <Gate />
          </AuthProvider>
        </ConfirmProvider>
      </ToastProvider>
    </QueryClientProvider>
  </StrictMode>,
);
