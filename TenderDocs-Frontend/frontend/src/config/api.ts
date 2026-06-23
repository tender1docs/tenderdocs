/**
 * Central API access for the TenderDocs frontend.
 *
 * - Base URL comes from VITE_API_BASE_URL (defaults to "/api", served same-origin
 *   through the nginx gateway in Docker and through the Vite dev proxy locally).
 * - Auth is explicit: the user signs in with email/password or Google (see the auth
 *   provider + login screen). Tokens are cached in localStorage and refreshed
 *   automatically on 401; a failed refresh clears the session.
 */

export const API_BASE_URL: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') || '/api';

const DEMO_EMAIL = (import.meta.env.VITE_DEMO_EMAIL as string | undefined) || 'admin@tenderdocs.io';
const DEMO_PASSWORD = (import.meta.env.VITE_DEMO_PASSWORD as string | undefined) || 'Admin@12345';

const ACCESS_KEY = 'td_access_token';
const REFRESH_KEY = 'td_refresh_token';
const PERSIST_KEY = 'td_remember';

interface Tokens { accessToken: string; refreshToken: string }

interface AuthResult {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  user: unknown;
}

// "Remember me" → persist in localStorage (survives browser restart).
// Otherwise → sessionStorage (cleared when the tab/window closes).
function stores(): Storage[] {
  const out: Storage[] = [];
  try { out.push(window.localStorage); } catch { /* unavailable */ }
  try { out.push(window.sessionStorage); } catch { /* unavailable */ }
  return out;
}

function readTokens(): Tokens | null {
  for (const s of stores()) {
    try {
      const accessToken = s.getItem(ACCESS_KEY);
      const refreshToken = s.getItem(REFRESH_KEY);
      if (accessToken && refreshToken) return { accessToken, refreshToken };
    } catch { /* ignore */ }
  }
  return null;
}

function writeTokens(t: Tokens, remember = true) {
  // Clear both stores first so tokens never linger in the wrong place.
  clearTokens();
  try {
    const target = remember ? window.localStorage : window.sessionStorage;
    target.setItem(ACCESS_KEY, t.accessToken);
    target.setItem(REFRESH_KEY, t.refreshToken);
    target.setItem(PERSIST_KEY, remember ? '1' : '0');
  } catch {
    /* storage unavailable — fall back to in-memory only */
  }
  memoryTokens = t;
}

function clearTokens() {
  for (const s of stores()) {
    try {
      s.removeItem(ACCESS_KEY);
      s.removeItem(REFRESH_KEY);
      s.removeItem(PERSIST_KEY);
    } catch { /* ignore */ }
  }
  memoryTokens = null;
}

// Whether the active session was started with "remember me".
function rememberPref(): boolean {
  for (const s of stores()) {
    try { const v = s.getItem(PERSIST_KEY); if (v !== null) return v === '1'; } catch { /* ignore */ }
  }
  return true;
}

let memoryTokens: Tokens | null = readTokens();

export class ApiError extends Error {
  status: number;
  body: unknown;
  constructor(status: number, message: string, body?: unknown) {
    super(message);
    this.status = status;
    this.body = body;
  }
}

async function rawFetch(path: string, init: RequestInit): Promise<Response> {
  const url = path.startsWith('http') ? path : `${API_BASE_URL}${path}`;
  return fetch(url, init);
}

async function parseError(res: Response): Promise<ApiError> {
  let body: unknown;
  let message = `${res.status} ${res.statusText}`;
  try {
    body = await res.json();
    const b = body as { title?: string; detail?: string; message?: string };
    message = b.detail || b.title || b.message || message;
  } catch { /* non-JSON */ }
  return new ApiError(res.status, message, body);
}

// ---- Auth ----------------------------------------------------------------

async function loginRequest(email: string, password: string): Promise<AuthResult> {
  const res = await rawFetch('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw await parseError(res);
  return res.json();
}

/** Email + password sign-in. Stores tokens on success. */
export async function loginWithPassword(email: string, password: string, remember = true): Promise<void> {
  const result = await loginRequest(email, password);
  writeTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken }, remember);
}

/** Create a new workspace + admin user, then sign in. */
export async function registerAccount(
  email: string, password: string, fullName: string, organizationName?: string, remember = true,
): Promise<void> {
  const res = await rawFetch('/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, fullName, organizationName }),
  });
  if (!res.ok) throw await parseError(res);
  const result = (await res.json()) as AuthResult;
  writeTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken }, remember);
}

/** Sign in with a Google id_token (from Google Identity Services). Creates the account on first use.
 *  Returns the user's current role string ("Approver" | "Uploader" | "Viewer"). */
export async function loginWithGoogleIdToken(idToken: string, remember = true): Promise<string | undefined> {
  const res = await rawFetch('/auth/google', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ idToken }),
  });
  if (!res.ok) throw await parseError(res);
  const result = (await res.json()) as AuthResult;
  writeTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken }, remember);
  return (result.user as { role?: string } | undefined)?.role;
}

/** Self-assign an access role (demo). Stores the refreshed tokens and returns the new role string. */
export async function selectRoleRequest(role: string): Promise<string | undefined> {
  const res = await authedFetch('/auth/role', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ role }),
  });
  if (!res.ok) throw await parseError(res);
  const result = (await res.json()) as AuthResult;
  writeTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken }, rememberPref());
  return (result.user as { role?: string } | undefined)?.role;
}

export function isAuthenticated(): boolean {
  return !!(memoryTokens ?? readTokens());
}

/** Default demo credentials, surfaced to the login screen for one-click sign-in. */
export const DEMO_CREDENTIALS = { email: DEMO_EMAIL, password: DEMO_PASSWORD };

async function refreshTokens(): Promise<Tokens | null> {
  const current = memoryTokens ?? readTokens();
  if (!current) return null;
  try {
    const res = await rawFetch('/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: current.refreshToken }),
    });
    if (!res.ok) return null;
    const result = (await res.json()) as AuthResult;
    const t = { accessToken: result.accessToken, refreshToken: result.refreshToken };
    writeTokens(t, rememberPref());
    return t;
  } catch {
    return null;
  }
}

/** Manually set tokens (e.g. after an external auth flow). */
export function setAuthTokens(accessToken: string, refreshToken: string) {
  writeTokens({ accessToken, refreshToken });
}
/** Clear the session (logout). */
export function clearAuth() {
  clearTokens();
}

// ---- Core request ---------------------------------------------------------

async function authedFetch(path: string, init: RequestInit, retry = true): Promise<Response> {
  let tokens = memoryTokens ?? readTokens();
  if (!tokens) throw new ApiError(401, 'Not authenticated');

  const headers = new Headers(init.headers);
  headers.set('Authorization', `Bearer ${tokens.accessToken}`);

  let res = await rawFetch(path, { ...init, headers });

  if (res.status === 401 && retry) {
    const refreshed = await refreshTokens();
    if (!refreshed) {
      clearTokens();
      throw new ApiError(401, 'Session expired');
    }
    const retryHeaders = new Headers(init.headers);
    retryHeaders.set('Authorization', `Bearer ${refreshed.accessToken}`);
    res = await rawFetch(path, { ...init, headers: retryHeaders });
  }

  return res;
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const init: RequestInit = { method };
  if (body !== undefined) {
    init.headers = { 'Content-Type': 'application/json' };
    init.body = JSON.stringify(body);
  }
  const res = await authedFetch(path, init);
  if (!res.ok) throw await parseError(res);
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, body),
  del: <T>(path: string, body?: unknown) => request<T>('DELETE', path, body),

  /** Multipart upload (FormData). */
  async upload<T>(path: string, form: FormData): Promise<T> {
    const res = await authedFetch(path, { method: 'POST', body: form });
    if (!res.ok) throw await parseError(res);
    const text = await res.text();
    return (text ? JSON.parse(text) : undefined) as T;
  },

  /** Fetch binary content as a Blob (file/zip downloads). */
  async blob(path: string): Promise<Blob> {
    const res = await authedFetch(path, { method: 'GET' });
    if (!res.ok) throw await parseError(res);
    return res.blob();
  },
};

/** Trigger a browser download for a Blob with the given filename. */
export function saveBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
