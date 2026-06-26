# TenderDocs — Pre-Deployment Confidentiality Checklist

**Read and complete this before any production deployment.** It lists every value that
must be changed away from its development default, so no demo secret, demo account, or
someone else's credential ever reaches production. Work top to bottom; tick each box.

> Golden rule: **secrets live only in the `.env` file on the server**, never in Git.
> `.gitignore` already excludes `**/.env`, so a real `.env` is never committed — but the
> values still have to be *rotated* from the examples below.

---

## 0. Files involved

| File | Committed to Git? | What it holds |
| --- | --- | --- |
| `deploy/.env` | **No** (you create it from `.env.example`) | All production secrets for the server build |
| `deploy/.env.example` | Yes (placeholders only) | Template — no real values |
| `TenderDocs-Frontend/frontend/.env` | **No** | SPA build-time values (API URL, Google client id) |
| `TenderDocs-Backend/.env` | **No** | Local-dev secrets (do **not** ship this to prod) |
| `appsettings.Development.json` | Yes | Dev-only secrets, clearly marked `change-me`; never used in Production |

The production stack is `deploy/docker-compose.server.yml` and reads `deploy/.env`.

---

## 1. Generate fresh secrets (REQUIRED)

Run these on the server (Linux/macOS) and paste each value into `deploy/.env`:

```bash
echo "POSTGRES_PASSWORD=$(openssl rand -base64 24)"
echo "JWT_SECRET=$(openssl rand -base64 48)"
echo "ENCRYPTION_KEY=$(openssl rand -base64 32)"
```

- [ ] `POSTGRES_PASSWORD` — replaced (was `CHANGE_ME_long_random`)
- [ ] `JWT_SECRET` — replaced. Rotating this later logs everyone out (invalidates all tokens).
- [ ] `ENCRYPTION_KEY` — replaced. **Set this once and never change it after go-live.**
      It encrypts stored Google Drive / S3 credentials at rest; changing it makes
      previously-saved storage connections undecryptable and they'd need reconnecting.

> ⚠️ Use **different** secrets per environment (staging ≠ production). Never reuse the
> values from `TenderDocs-Backend/.env` (those are known dev values).

---

## 2. Domain & URLs (REQUIRED)

In `deploy/.env`:

- [ ] `DOMAIN=tenderdocs.yourdomain.com` → your real host (no `https://`)
- [ ] `PUBLIC_URL=https://tenderdocs.yourdomain.com` → same host **with** `https://`
      (this becomes the single allowed CORS origin)
- [ ] DNS A/AAAA record for `DOMAIN` points at the server, ports **80** and **443** open.

Caddy obtains the HTTPS certificate automatically for `DOMAIN` — no manual TLS steps.

---

## 3. Google Sign-In / Drive (REQUIRED for login)

The repo previously shipped a real OAuth **client id** in `deploy/.env.example`; it has been
replaced with a placeholder. **Use your client's own Google Cloud project.**

In **Google Cloud Console → APIs & Services → Credentials → OAuth 2.0 Client ID**:

- [ ] Create (or use the client's) OAuth client. Under **Authorized JavaScript origins** add
      `https://tenderdocs.yourdomain.com`. Under **Authorized redirect URIs** add
      `https://tenderdocs.yourdomain.com/api/google-drive/callback`.

In `deploy/.env`:

- [ ] `GOOGLE_CLIENT_ID` → your client id
- [ ] `GOOGLE_CLIENT_SECRET` → your client secret (**secret — never commit**)
- [ ] `GOOGLE_REDIRECT_URI` → `https://tenderdocs.yourdomain.com/api/google-drive/callback`
- [ ] `GOOGLE_DRIVE_FOLDER_ID` → the Drive folder uploads should land in (only if using Drive)

In `TenderDocs-Frontend/frontend/.env` (baked into the SPA at build time):

- [ ] `VITE_GOOGLE_CLIENT_ID` → **must equal** the backend `GOOGLE_CLIENT_ID` above
      (leave empty to hide the "Sign in with Google" button)

> Storage choice: documents live on the server's `storage` volume (**Local storage**, pairs with
> the 500 GB disk) until an Admin connects **Google Drive** from Settings → Storage. Note: the
> Google **client id** is always required (it's how everyone signs in); the Drive secret/folder are
> only needed to store files in Drive rather than locally.

---

## 4. Accounts & seeding (controlled access)

There are **no demo accounts and no demo data**. On a fresh database the seeder creates the
permission catalog and exactly **one** user — `SEED_ADMIN_EMAIL` — as **Admin** (Google sign-in
only, no password). Everyone else is created by that Admin in **Administration → Users**, and
**unknown Google accounts are rejected** (no self-registration).

- [ ] In `deploy/.env`, set `SEED_ADMIN_EMAIL` to the real Google account that will administer the
      app (default `tender1docs@gmail.com`), and keep `SEED_ENABLED=true`.
- [ ] The frontend has **no** prefilled credentials (the old `VITE_DEMO_*` are gone) — nothing to clear.
- [ ] After first sign-in, create your team in Administration → Users and assign project access.
      Roles: **Admin** (full), **Uploader** (document work), **Approver** (approve/reject only),
      **Viewer** (read-only).
- [ ] Reminder: each user's email must be a real Google account **and** allowed by your OAuth client
      (added under "Test users", or the app published) — see §3.

---

## 5. Production toggles

In `deploy/.env`:

- [ ] `SWAGGER_ENABLED=false` — keep the API explorer private in production.
- [ ] `SEED_ENABLED` — per your choice in §4.
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (already set in the compose file).

---

## 6. Email / SMTP (optional — only if you enable email notifications)

`SMTP_*` are blank by default (email simply doesn't send). If you want expiry reminders etc.,
set them in the API environment (and keep the password secret):

- [ ] `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM`

---

## 7. Document compression (already configured — optional tuning)

Compression is **on** by default at the `Balanced` level (≈50–85% smaller files; see the
backend README). To tune, set in the API environment:

- `Compression__Level` = `Light` | `Balanced` | `Maximum`
- `Compression__Enabled` = `true` | `false`

No secrets here — listed only so you know it's active and adjustable without a rebuild.

---

## 8. First real admin (when `SEED_ENABLED=false`)

With seeding off there is no account yet. Create the first admin by temporarily seeding
(Path B in §4), **or** insert one directly. Easiest: boot once with `SEED_ENABLED=true`,
change the admin password and email in-app, remove the other demo users, then turn seeding off.

---

## 9. Final verification (run before announcing the URL)

```bash
# 1) No placeholder/dev values remain in the production env:
grep -nE 'CHANGE_ME|yourdomain|YOUR_OAUTH|YOUR_DRIVE|dev-only|tenderdocs\.io' deploy/.env || echo "OK: no placeholders left"

# 2) No real .env is staged for commit (should print nothing):
git status --porcelain | grep -E '\.env$' && echo "STOP: a real .env is staged" || echo "OK: no .env staged"

# 3) Secrets are non-trivial length:
awk -F= '/^(POSTGRES_PASSWORD|JWT_SECRET|ENCRYPTION_KEY)=/{print $1": "length($2)" chars"}' deploy/.env
```

Then, in a browser:
- [ ] Sign-in page shows only the **Sign in with Google** button (no credentials).
- [ ] Signing in with a Google account the Admin has **not** added is **rejected** (no self-registration).
- [ ] HTTPS padlock is valid for your domain.
- [ ] `https://<domain>/swagger` is unreachable (Swagger disabled).
- [ ] A test upload succeeds and shows the "compressed" size badge.

---

## 10. Operational confidentiality (post-deploy)

- [ ] **Backups**: schedule nightly `pg_dump` + a copy of the `storage` volume to off-box
      storage (provider snapshot or `restic`/`rclone`). Document where backups live and who can read them.
- [ ] **Access**: restrict SSH to keys only; limit who holds the `deploy/.env`.
- [ ] **Rotation**: rotate `JWT_SECRET` / DB / SMTP passwords on staff offboarding
      (remember: not `ENCRYPTION_KEY` once data exists — see §1).
- [ ] **Data residency**: if the client requires Indian data residency for tender documents,
      host in an India region — see `HOSTING-AND-COSTS.md`.
