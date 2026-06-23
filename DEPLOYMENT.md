# TenderDocs — Deployment Readiness & Hosting Strategy (Phase 2)

This document is the deployment **plan** for review before any production secrets are
provisioned. It covers the hosting recommendation, CI/CD, environment strategy, a
production readiness report, a security review, cost estimates, and a step-by-step
deployment guide. Implementation artifacts referenced here are already in the repo:

| Artifact | Path |
| --- | --- |
| CI/CD pipeline | `.github/workflows/ci-cd.yml` |
| Production compose | `deploy/docker-compose.prod.yml` |
| TLS reverse proxy | `deploy/Caddyfile` |
| Production env template | `TenderDocs-Backend/.env.production.example` |
| Secret exclusion | `.gitignore` |

---

## 1. Deployment readiness assessment (summary)

The application is in good shape to deploy. It is fully containerized, builds cleanly,
and already has the production-relevant plumbing (configurable CORS, structured logging,
health checks, hashed passwords, a pluggable storage provider).

**Ready now**
- Backend (ASP.NET Core 8) + frontend (React/TS/Vite) both build and ship as Docker images.
- Single `docker compose` brings up Postgres, API, frontend, and a gateway.
- Serilog request/structured logging; `/health` health check; Postgres healthcheck; restart policies.
- JWT auth with a SHA-256-derived signing key; BCrypt password hashing; configurable CORS origins.
- Storage abstraction (Local + Google Drive) selectable at runtime.

**Must address before/at go-live** (details in §6 and §8)
- Dev secrets are currently committed in `TenderDocs-Backend/.env` — rotate and stop tracking.
- TLS is terminated outside the app — add a reverse proxy with automatic HTTPS (Caddy, included).
- No request rate limiting yet — recommended hardening before public exposure.
- No automated DB backup yet — add `pg_dump` cron or provider snapshots.

---

## 2. Recommended hosting platform

**Recommendation: Option A — GitHub Actions + a single VPS (Hetzner), behind Caddy for automatic HTTPS.**

Rationale against the stated goals:

- **Low cost** — a 4 vCPU / 8 GB Hetzner CX32 (~€6.80/mo, traffic included) comfortably runs
  the whole stack (Postgres + API + frontend + proxy). The smaller CX22 (2 vCPU / 4 GB, ~€4/mo)
  is enough for a review environment.
- **Easy maintenance** — you deploy the *same* `docker compose` you already run locally; no
  rewrite, no platform-specific config.
- **Reliable** — long-running container host, no cold starts, predictable flat price.
- **Suitable for test users** — custom domain + HTTPS + seeded demo accounts (§9).
- **Easy future scaling** — scale vertically first (resize the VPS in a few minutes), then
  horizontally (move Postgres to managed, run multiple API replicas behind the proxy).
- **Docker / PostgreSQL / custom domain / HTTPS** — all natively supported.

**Runner-up: Option B — Railway**, if you would rather not manage a server at all. It is the
least-effort PaaS, but the four-service compose (api, frontend, proxy, db) maps onto per-service
usage billing, so the monthly cost is higher and less predictable than a single VPS.

---

## 3. Hosting comparison (A–F)

Costs are approximate **monthly** figures for a small always-on app (1 API, 1 frontend, 1
Postgres) at review/early-production scale, verified against 2026 provider pricing. VPS prices
are all-inclusive of generous traffic; PaaS prices add metered compute/egress on top.

| Option | Example | Est. monthly | Setup complexity | Scalability | Notes |
| --- | --- | --- | --- | --- | --- |
| **A. GitHub Actions + VPS** | Hetzner CX22/CX32 | **~€4–7** (~$5–8) | Medium (one-time server setup) | Vertical now, horizontal later | Cheapest; run existing compose; you manage OS patching & backups |
| | DigitalOcean droplet | ~$12–18 | Low–Medium (polished UX) | Vertical; managed DB add-on | Per-second billing, monthly cap; nice dashboard |
| | Vultr / Contabo / Hostinger | ~$5–12 | Medium | Vertical | Contabo cheapest specs/€ but variabler performance |
| **B. Railway** | — | ~$10–20 | Very low | Automatic, usage-based | Fast Git deploys; multi-service cost adds up; no always-on free tier |
| **C. Render** | — | ~$14–21 | Low | Per-service plans | Free tier sleeps after 15 min; Starter $7/service; Postgres $7+ |
| **D. Fly.io** | — | ~$13–25 | Medium | Per-machine, multi-region | No free tier for new accounts; metered egress + several add-on costs |
| **E. Azure** | App Service + Postgres Flexible | ~$40–80+ | High | Excellent (enterprise) | Overkill/expensive at this scale; best when you need Azure compliance/scale |
| **F. AWS** | ECS/EC2 + RDS + ALB | ~$50–100+ | High | Excellent (enterprise) | Most powerful, most moving parts; premature for a review app |

**Bottom line:** A VPS (Hetzner) is the best fit for *this* app right now. Railway is the
sensible no-ops alternative. Azure/AWS are right only once enterprise scale or compliance forces them.

---

## 4. Recommended deployment architecture

```
                    Internet (HTTPS :443)
                           │
                    ┌──────▼───────┐
                    │    Caddy     │  auto TLS (Let's Encrypt) for $DOMAIN
                    │  (gateway)   │  /api,/swagger,/health → api ; else → frontend
                    └──┬───────┬───┘
              /api/*   │       │  / (SPA)
                 ┌─────▼──┐  ┌─▼─────────┐
                 │  api   │  │ frontend  │  (nginx serving the built Vite SPA)
                 │ :8080  │  │   :80     │
                 └───┬────┘  └───────────┘
                     │
               ┌─────▼─────┐
               │ postgres  │  (named volume: pgdata)
               └───────────┘

   Storage: ./storage volume (Local provider) and/or Google Drive provider.
   All four services run via deploy/docker-compose.prod.yml on one VPS.
```

Images are built and pushed to GHCR by CI; the VPS only pulls and runs them.

---

## 5. CI/CD pipeline (`.github/workflows/ci-cd.yml`)

Trigger: every push/PR builds and tests; **push to `main`** additionally publishes images and
(optionally) deploys.

- **backend** — `setup-dotnet@8`, `dotnet restore`, `dotnet build -c Release`, `dotnet test`.
  (A `*.Tests` project should be added to the solution; the step is wired and ready for it.)
- **frontend** — Node 20, `npm ci`, **lint** via `tsc --noEmit` (no ESLint config present yet),
  `npm run build`.
- **publish** (main only) — builds both Docker images and pushes to **GHCR**
  (`ghcr.io/<owner>/<repo>-backend` and `-frontend`) using the built-in `GITHUB_TOKEN`
  (no extra registry secret needed). Build cache via GitHub Actions cache.
- **deploy** (main only, **gated**) — runs only when repository variable `DEPLOY_ENABLED=true`.
  SSHes to the VPS and runs `docker compose -f docker-compose.prod.yml pull && up -d`.

**Secrets/variables to set in GitHub when you approve deployment**

| Name | Type | Purpose |
| --- | --- | --- |
| `DEPLOY_ENABLED` | variable | set to `true` to turn the deploy job on |
| `VPS_HOST` | secret | server IP/hostname |
| `VPS_USER` | secret | SSH user |
| `VPS_SSH_KEY` | secret | private key for that user |
| `VPS_APP_DIR` | secret | path containing `docker-compose.prod.yml` + `.env` |

`push to main → automatic deployment` is satisfied: merging to `main` builds → publishes → deploys.

---

## 6. Environment variable strategy + secret audit

**Strategy:** no secret literals in code or compose. All secrets come from a `.env` file that
lives only on the server (and in GitHub Actions secrets for deploy), templated by
`TenderDocs-Backend/.env.production.example`. `.gitignore` now excludes every `.env` except the
`*.example` templates.

**Secret audit**

| Secret | Current state | Action |
| --- | --- | --- |
| `JWT_SECRET` | committed dev value in `TenderDocs-Backend/.env` | **Rotate.** Generate `openssl rand -base64 48` for prod |
| Google OAuth (`client id`/`secret`) | committed dev values | **Rotate the client secret** in Google Cloud; client id is public by design |
| DB connection string / `POSTGRES_PASSWORD` | committed dev value | **Rotate.** Use `openssl rand -base64 24` |
| `ENCRYPTION_KEY` (protects stored storage creds) | committed dev value | **Rotate.** `openssl rand -base64 32` |

**Important:** because `.env` was committed, adding it to `.gitignore` is not enough — untrack it
and rotate the values:

```bash
git rm --cached TenderDocs-Backend/.env
git commit -m "Stop tracking .env; secrets move to server-side env"
# then rotate every value above and put real values only in the server .env
```

---

## 7. Production readiness report

| Area | Status | Notes |
| --- | --- | --- |
| Containerization | ✅ | Multi-stage Dockerfiles, single compose |
| Build/CI | ✅ | Frontend builds; backend compiled by the new CI pipeline on push |
| HTTPS/TLS | ⚠️ → ✅ with Caddy | Provided in `deploy/` (auto Let's Encrypt) |
| Database | ✅ | Postgres 16 + healthcheck + named volume |
| DB backups | ❌ | Add `pg_dump` cron or VPS/managed snapshots (§8) |
| Secrets management | ⚠️ | Templated + gitignored; **rotate committed dev secrets** |
| Logging | ✅ | Serilog structured + request logging |
| Error reporting | ❌ | Optional: add Sentry/GlitchTip DSN |
| Uptime monitoring | ❌ | Optional: UptimeRobot/BetterStack on `/health` |
| Rate limiting | ❌ | Recommended: ASP.NET `AddRateLimiter` on auth endpoints |
| Auth/roles | ✅ | JWT + Admin/Manager/Viewer; admin user creation added |

---

## 8. Security review

**Strengths:** BCrypt password hashing; JWT signing key SHA-256-derived (tolerates shorter raw
keys); CORS restricted to configured origins; Swagger gated by config (off in prod via env);
health endpoint; no secrets in source after the §6 cleanup.

**Findings & recommendations**

1. **Committed dev secrets** (high) — rotate all of them (§6); they have appeared in shared zips.
2. **No rate limiting** (medium) — add `builder.Services.AddRateLimiter(...)` and
   `app.UseRateLimiter()`, especially a fixed/sliding window on `/api/auth/*` to blunt brute force.
3. **Token storage = localStorage/sessionStorage** (medium) — convenient and CSRF-resistant, but
   readable by injected scripts (XSS). Acceptable for review; for higher assurance, move to
   httpOnly, `Secure`, `SameSite` cookies and add CSRF protection. ("Secure cookies" in the
   checklist refers to this option.)
4. **TLS** (resolved by Caddy) — ensure HTTP→HTTPS redirect (Caddy does this automatically) and
   that the app trusts the proxy for correct scheme if it ever emits absolute URLs.
5. **Swagger in prod** — keep `SWAGGER_ENABLED=false` in the production `.env`.
6. **Seeding in prod** — `SEED_ENABLED=true` is useful to create the review/demo data once; set it
   to `false` after the review so demo accounts aren't recreated.

---

## 9. Test user preparation (Task 5)

**Implemented in this phase**

- **Admin can create users** — `POST /api/users` (Admin only) → `CreateUserCommand`
  (`Features/Users/CreateUser.cs`); client method `UsersApi.create(...)`.
- **Profile editing works** — `PUT /api/auth/me` → `UpdateProfileCommand`
  (`Features/Auth/UpdateProfile.cs`); the Settings "Save changes" button now persists the name
  and refreshes the cached profile.
- **Sign in / sign up / logout** — already implemented (login page, register, JWT, logout that
  clears tokens and query cache).
- **Roles / permissions / protected routes** — `UserRole { Admin, Manager, Viewer }`; API actions
  use `[Authorize]`/`[Authorize(Roles="Admin")]`; the frontend gates the app behind authentication.

**Seeded demo accounts** (created on first run when `SEED_ENABLED=true`, alongside demo
organization, folders, documents, projects, and notifications):

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@tenderdocs.io` | `Admin@12345` |
| Reviewer (Manager) | `reviewer@tenderdocs.io` | `Reviewer@12345` |
| Viewer | `viewer@tenderdocs.io` | `Viewer@12345` |

> Rotate or disable these before any public/production use.

---

## 10. Step-by-step deployment guide (VPS)

**Prerequisites:** a VPS (e.g. Hetzner CX22/CX32, Ubuntu 24.04), a domain you can point at it,
and (optionally) Google OAuth credentials.

1. **Provision the server** and create a non-root sudo user; add your SSH key.
2. **Install Docker + Compose plugin:**
   ```bash
   curl -fsSL https://get.docker.com | sh
   sudo usermod -aG docker $USER   # re-login afterwards
   ```
3. **Point DNS:** create an `A` record for `tenderdocs.example.com` → the server IP. Open ports
   `80` and `443` in the firewall.
4. **Get the deployment files on the server:**
   ```bash
   sudo mkdir -p /opt/tenderdocs && sudo chown $USER /opt/tenderdocs && cd /opt/tenderdocs
   # copy deploy/docker-compose.prod.yml and deploy/Caddyfile here
   ```
5. **Create the production `.env`** from the template and fill in real, rotated values:
   ```bash
   cp .env.production.example .env
   # set DOMAIN, PUBLIC_URL, GHCR_REPO, POSTGRES_PASSWORD,
   #     JWT_SECRET (openssl rand -base64 48),
   #     ENCRYPTION_KEY (openssl rand -base64 32), Google_* if used
   ```
6. **Authenticate to GHCR** (images are private by default) and start:
   ```bash
   echo "<GHCR_PAT>" | docker login ghcr.io -u <github-user> --password-stdin
   docker compose -f docker-compose.prod.yml pull
   docker compose -f docker-compose.prod.yml up -d
   ```
   Caddy obtains a Let's Encrypt certificate automatically once DNS resolves.
7. **Verify:** `https://tenderdocs.example.com/health` returns healthy; the SPA loads; log in with
   a demo account.
8. **Enable auto-deploy:** in GitHub, add the secrets/variable from §5 and set `DEPLOY_ENABLED=true`.
   From then on, **push to `main`** rebuilds, publishes, and redeploys automatically.

---

## 11. Production checklist

**Infrastructure**
- [ ] DNS `A` record → server; ports 80/443 open
- [ ] HTTPS via Caddy (auto Let's Encrypt); HTTP→HTTPS redirect confirmed
- [ ] SSL certificate auto-renewal working (Caddy handles it)
- [ ] PostgreSQL backups: nightly `pg_dump` to off-box storage, e.g.
      `docker compose exec -T postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB | gzip > backup-$(date +%F).sql.gz`
      via cron, plus periodic VPS snapshots; test a restore
- [ ] Named volumes (`pgdata`, `storage`) on persistent disk

**Security**
- [ ] All committed dev secrets rotated; `.env` untracked
- [ ] `JWT_SECRET` ≥ 32 random bytes; sensible issuer/audience
- [ ] CORS `AllowedOrigins` = your public URL only
- [ ] Rate limiting added on auth endpoints (recommended)
- [ ] Secure-cookie option evaluated for token storage (XSS hardening)
- [ ] Password storage = BCrypt (already in place)
- [ ] `SWAGGER_ENABLED=false`, `SEED_ENABLED=false` after the review

**Monitoring**
- [ ] Application logs shipped/retained (Serilog → file/stdout; optional log drain)
- [ ] Error reporting (optional: Sentry/GlitchTip DSN)
- [ ] Uptime monitor hitting `/health` (optional: UptimeRobot/BetterStack)

---

## 12. Recommended next hardening (post-approval, small additions)

- Add ASP.NET rate limiting (auth endpoints first).
- Add a `*.Tests` project so the CI `dotnet test` step runs real tests.
- Add a Team-page "Add user" form (the `POST /users` API and `UsersApi.create` client are ready).
- Wire an error-reporting sink and an uptime monitor.
