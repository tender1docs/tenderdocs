import {
  createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode,
} from 'react';
import {
  isAuthenticated, loginWithGoogleIdToken, selectRoleRequest, clearAuth,
} from '@/config/api';
import { AuthApi } from '@/services/api';
import { normalizeRole, type Role } from '@/types';

export const GOOGLE_CLIENT_ID =
  (import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined) || '';

interface AuthContextValue {
  authed: boolean;
  googleEnabled: boolean;
  role: Role | null;            // null while the session's role is still loading
  needsRolePick: boolean;       // true right after sign-in / "Switch role"
  loginGoogle: (idToken: string, remember?: boolean) => Promise<void>;
  selectRole: (role: Role) => Promise<void>;
  switchRole: () => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [authed, setAuthed] = useState<boolean>(() => isAuthenticated());
  const [role, setRole] = useState<Role | null>(null);
  const [needsRolePick, setNeedsRolePick] = useState<boolean>(false);

  // On an already-authenticated session (e.g. a page refresh), load the current role.
  useEffect(() => {
    if (!authed || role) return;
    let cancelled = false;
    AuthApi.me()
      .then((u) => { if (!cancelled) setRole(normalizeRole(u.role)); })
      .catch(() => {
        // The stored token is stale/invalid (e.g. a user from a previous database) — the role can't
        // be loaded, so clear the dead session and return to the login screen instead of hanging.
        if (cancelled) return;
        clearAuth();
        setAuthed(false);
        setRole(null);
        setNeedsRolePick(false);
      });
    return () => { cancelled = true; };
  }, [authed, role]);

  const loginGoogle = useCallback(async (idToken: string, remember = true) => {
    const r = await loginWithGoogleIdToken(idToken, remember);
    setRole(normalizeRole(r));
    setAuthed(true);
    setNeedsRolePick(true);   // always choose how to use the app after signing in
  }, []);

  const selectRole = useCallback(async (next: Role) => {
    const r = await selectRoleRequest(next);
    setRole(normalizeRole(r ?? next));
    setNeedsRolePick(false);
  }, []);

  const switchRole = useCallback(() => setNeedsRolePick(true), []);

  const logout = useCallback(() => {
    clearAuth();
    setAuthed(false);
    setRole(null);
    setNeedsRolePick(false);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    authed,
    googleEnabled: !!GOOGLE_CLIENT_ID,
    role,
    needsRolePick,
    loginGoogle,
    selectRole,
    switchRole,
    logout,
  }), [authed, role, needsRolePick, loginGoogle, selectRole, switchRole, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

/** Lazily loads the Google Identity Services script once. Resolves false if unavailable. */
let gsiPromise: Promise<boolean> | null = null;
export function loadGoogleIdentity(): Promise<boolean> {
  if (!GOOGLE_CLIENT_ID) return Promise.resolve(false);
  if (typeof window !== 'undefined' && (window as unknown as { google?: unknown }).google) {
    return Promise.resolve(true);
  }
  if (gsiPromise) return gsiPromise;
  gsiPromise = new Promise<boolean>((resolve) => {
    const s = document.createElement('script');
    s.src = 'https://accounts.google.com/gsi/client';
    s.async = true;
    s.defer = true;
    s.onload = () => resolve(true);
    s.onerror = () => resolve(false);
    document.head.appendChild(s);
  });
  return gsiPromise;
}
