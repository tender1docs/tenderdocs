import { useEffect, useRef, useState } from 'react';
import { Logo } from '@/components/layout/Logo';
import { useAuth, loadGoogleIdentity, GOOGLE_CLIENT_ID } from '@/auth/AuthProvider';

const TAGS = ['GST', 'PAN', 'ITR', 'MSME', 'Balance Sheet', 'Tender Form'];

export default function LoginPage() {
  const { loginGoogle, googleEnabled } = useAuth();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const googleBtnRef = useRef<HTMLDivElement>(null);

  // Render the Google Identity Services button.
  useEffect(() => {
    if (!googleEnabled) return;
    let cancelled = false;
    loadGoogleIdentity().then((ok) => {
      if (!ok || cancelled) return;
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const g = (window as any).google;
      if (!g?.accounts?.id || !googleBtnRef.current) return;
      g.accounts.id.initialize({
        client_id: GOOGLE_CLIENT_ID,
        callback: async (resp: { credential?: string }) => {
          if (!resp.credential) return;
          setError(null);
          setBusy(true);
          try {
            await loginGoogle(resp.credential, true);
          } catch (e) {
            setError(e instanceof Error ? e.message : 'Google sign-in failed');
            setBusy(false);
          }
        },
      });
      g.accounts.id.renderButton(googleBtnRef.current, {
        theme: 'outline', size: 'large', width: 320, text: 'continue_with',
      });
    });
    return () => { cancelled = true; };
  }, [googleEnabled, loginGoogle]);

  return (
    <div className="grid min-h-screen grid-cols-1 bg-canvas dark:bg-[#0B0F12] lg:grid-cols-2">
      {/* Sign-in */}
      <div className="flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm">
          <Logo />
          <div className="surface mt-8 p-6">
            <h1 className="text-2xl font-bold tracking-tight text-ink dark:text-slate-100">Welcome</h1>
            <p className="mt-1 text-sm text-ink-muted">
              Sign in with your Google account to access your tender document workspace.
            </p>

            <div className="mt-6 flex flex-col items-center gap-3">
              {googleEnabled ? (
                <div className="flex min-h-[44px] justify-center">
                  <div ref={googleBtnRef} />
                </div>
              ) : (
                <p className="rounded-lg bg-warn-bg px-3 py-2 text-sm text-warn-text">
                  Google sign-in isn’t configured. Set <code>VITE_GOOGLE_CLIENT_ID</code> (frontend) and
                  <code> Google:ClientId</code> (backend) to enable it.
                </p>
              )}
              {busy && <p className="text-sm text-ink-muted">Signing you in…</p>}
              {error && <p className="w-full rounded-lg bg-danger-bg px-3 py-2 text-sm text-danger-text">{error}</p>}
            </div>

            <p className="mt-6 text-center text-xs text-ink-faint">
              You’ll choose your role (Uploader, Approver or Viewer) right after signing in.
            </p>
          </div>
        </div>
      </div>

      {/* Marketing panel */}
      <div className="hidden flex-col justify-center bg-gradient-to-br from-brand-50 to-canvas px-12 dark:from-brand-900/20 dark:to-[#0B0F12] lg:flex">
        <h2 className="max-w-md text-4xl font-bold leading-tight tracking-tight text-ink dark:text-slate-100">
          One source of truth for every tender document.
        </h2>
        <p className="mt-4 max-w-md text-ink-muted">
          Classify GST, PAN, ITRs, balance sheets and tender forms with rich metadata. Find what you
          need in seconds. Bundle a project as a ZIP and ship the bid.
        </p>
        <div className="mt-8 grid max-w-md grid-cols-2 gap-3">
          {TAGS.map((t) => (
            <div key={t} className="surface px-4 py-3 text-sm font-medium text-ink dark:text-slate-200">{t}</div>
          ))}
        </div>
      </div>
    </div>
  );
}
