import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { ToastProvider } from '@/components/ui';
import { AuthProvider, useAuth } from '@/auth/AuthProvider';
import { lazy, Suspense, useEffect } from 'react';
import { router } from '@/routes';
import './styles.css';

const LoginPage = lazy(() => import('@/pages/LoginPage'));
const RoleSelectPage = lazy(() => import('@/pages/RoleSelectPage'));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, refetchOnWindowFocus: false, retry: 1 },
  },
});

const ScreenFallback = () => <div className="min-h-screen bg-canvas dark:bg-[#0B0F12]" />;

/** Login → role picker → app. */
function Gate() {
  const { authed, role, needsRolePick } = useAuth();
  // Drop any cached data when the session ends so the next sign-in starts clean.
  useEffect(() => { if (!authed) queryClient.clear(); }, [authed]);
  if (!authed) {
    return <Suspense fallback={<ScreenFallback />}><LoginPage /></Suspense>;
  }
  if (needsRolePick) {
    return <Suspense fallback={<ScreenFallback />}><RoleSelectPage /></Suspense>;
  }
  if (!role) {
    return <ScreenFallback />;   // briefly loading the session's role
  }
  return <RouterProvider router={router} />;
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AuthProvider>
          <Gate />
        </AuthProvider>
      </ToastProvider>
    </QueryClientProvider>
  </StrictMode>,
);
