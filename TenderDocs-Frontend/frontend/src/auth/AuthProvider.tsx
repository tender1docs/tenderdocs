import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  isAuthenticated,
  loginWithGoogleIdToken,
  clearAuth,
} from "@/config/api";
import { AuthApi } from "@/services/api";
import { normalizeRole, type Role } from "@/types";

export const GOOGLE_CLIENT_ID =
  (import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined) || "";

interface AuthContextValue {
  authed: boolean;
  googleEnabled: boolean;
  role: Role | null; // null while the session's role is still loading
  permissions: string[]; // resolved permission keys (source of truth for UI gating)
  ready: boolean; // true once role + permissions are loaded for the session
  loginGoogle: (idToken: string, remember?: boolean) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [authed, setAuthed] = useState<boolean>(() => isAuthenticated());
  const [role, setRole] = useState<Role | null>(null);
  const [permissions, setPermissions] = useState<string[]>([]);

  const clearSession = useCallback(() => {
    clearAuth();
    setAuthed(false);
    setRole(null);
    setPermissions([]);
  }, []);

  // On an already-authenticated session (e.g. a page refresh), load the role + permissions.
  useEffect(() => {
    if (!authed || role) return;
    let cancelled = false;
    AuthApi.me()
      .then((u) => {
        if (cancelled) return;
        setRole(normalizeRole(u.role));
        setPermissions(u.permissions ?? []);
      })
      .catch(() => {
        // The stored token is stale/invalid (e.g. a user from a previous database) — clear the dead
        // session and return to the login screen instead of hanging.
        if (!cancelled) clearSession();
      });
    return () => {
      cancelled = true;
    };
  }, [authed, role, clearSession]);

  const loginGoogle = useCallback(async (idToken: string, remember = true) => {
    const u = await loginWithGoogleIdToken(idToken, remember);
    setRole(normalizeRole(u.role));
    setPermissions(u.permissions ?? []);
    setAuthed(true);
  }, []);

  const logout = useCallback(() => clearSession(), [clearSession]);

  const value = useMemo<AuthContextValue>(
    () => ({
      authed,
      googleEnabled: !!GOOGLE_CLIENT_ID,
      role,
      permissions,
      ready: role !== null,
      loginGoogle,
      logout,
    }),
    [authed, role, permissions, loginGoogle, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

/** Lazily loads the Google Identity Services script once. Resolves false if unavailable. */
let gsiPromise: Promise<boolean> | null = null;
export function loadGoogleIdentity(): Promise<boolean> {
  if (!GOOGLE_CLIENT_ID) return Promise.resolve(false);
  if (
    typeof window !== "undefined" &&
    (window as unknown as { google?: unknown }).google
  ) {
    return Promise.resolve(true);
  }
  if (gsiPromise) return gsiPromise;
  gsiPromise = new Promise<boolean>((resolve) => {
    const s = document.createElement("script");
    s.src = "https://accounts.google.com/gsi/client";
    s.async = true;
    s.defer = true;
    s.onload = () => resolve(true);
    s.onerror = () => resolve(false);
    document.head.appendChild(s);
  });
  return gsiPromise;
}
