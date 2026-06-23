# TenderDocs

A full-stack tender-document manager: a React/TypeScript SPA backed by an ASP.NET Core 8 API,
PostgreSQL, and pluggable file storage — all wired together behind a single nginx gateway and
started with one command.

```
http://localhost:8080      → frontend (SPA)
http://localhost:8080/api  → backend API
http://localhost:8080/swagger → Swagger UI
```

---

## Architecture

```
                         ┌─────────────────────────────────────────┐
                         │            nginx gateway (:8080)         │
   Browser ───────────▶  │   /        → frontend SPA (:80)          │
                         │   /api     → API (:8080)                 │
                         │   /swagger → API                         │
                         │   /health  → API                         │
                         └───────────────┬──────────────┬──────────┘
                                         │              │
                              ┌──────────▼───┐   ┌──────▼───────────┐
                              │  frontend    │   │  api (ASP.NET 8) │
                              │  React + Vite│   │  CQRS / EF Core  │
                              │  served by   │   │  JWT / Swagger   │
                              │  nginx       │   └──────┬───────────┘
                              └──────────────┘          │
                                                 ┌──────▼───────┐   ┌──────────────┐
                                                 │ PostgreSQL 16│   │ file storage │
                                                 │  (pgdata)    │   │ (Local vol)  │
                                                 └──────────────┘   └──────────────┘
```

Single origin means **no CORS** in normal use and the SPA simply calls `/api`.

### Frontend
React 18 + TypeScript + Vite + Tailwind + React Query. Screens consume `I*Service` contracts
through React Query hooks. Typed API clients (`src/services/api`) call the backend and
`src/services/live.ts` maps backend DTOs to the frontend's view types — so the UI runs on real
data with no component rewrites. The app auto-authenticates against the seeded admin (no login
screen). See `TenderDocs-Frontend/frontend/README_FRONTEND.md`.

### Backend
ASP.NET Core 8, Clean Architecture (Domain / Application / Infrastructure / Api), CQRS via
MediatR, EF Core + PostgreSQL, JWT auth, Swagger. Endpoints cover auth, documents, projects,
folders, organize, notifications, storage, search, and dashboard. Migrations apply and demo data
seeds automatically on startup. See `TenderDocs-Backend/README_BACKEND.md`.

### Database
PostgreSQL 16. Schema is created by EF Core migrations on API startup. Core entities: organizations,
users, projects, folders (unlimited nesting via materialized path), documents, document↔project
assignments, requirements, tags, notifications, storage connections, audit logs.

### Storage
Provider-agnostic (`IStorageProvider`). **Local** filesystem in demo mode (persisted in the
`storage` Docker volume); **Google Drive** and **S3** are pluggable. Project export streams a ZIP
grouped into `GST / PAN / Financial / Technical / Others`.

### Docker
`docker compose` orchestrates `postgres`, `api`, `frontend`, and `nginx`. The frontend is built
from the sibling folder (`FRONTEND_CONTEXT`) and served behind the gateway.

---

## Repository layout

```
TenderDocs/
├── README.md                      # ← this file
├── TenderDocs-Backend/            # ASP.NET Core 8 API (run docker compose here)
│   ├── docker-compose.yml
│   ├── .env                       # secrets (pre-filled, ready to run)
│   ├── nginx/nginx.conf           # gateway
│   ├── README_BACKEND.md
│   └── src/ …
└── TenderDocs-Frontend/
    └── frontend/                  # React SPA (built automatically by compose)
        ├── Dockerfile
        ├── README_FRONTEND.md
        └── src/ …
```

---

## How the frontend talks to the backend

1. The SPA calls `VITE_API_BASE_URL` (default `/api`), same-origin through the gateway.
2. On first request `src/config/api.ts` logs in as the seeded admin
   (`admin@tenderdocs.io` / `Admin@12345`), caches the JWT, and refreshes it on 401.
3. Typed clients in `src/services/api` hit the API; `src/services/live.ts` maps DTOs to view
   types; React Query hooks feed the screens.

---

## Run in Docker (recommended)

```bash
cd TenderDocs/TenderDocs-Backend
docker compose up --build
```

Then open **http://localhost:8080**. The secrets are already in `TenderDocs-Backend/.env`, so no
extra configuration is required.

- **Seeded admin:** `admin@tenderdocs.io` / `Admin@12345` (the app auto-logs-in for you)
- Reset everything (fresh DB + reseed): `docker compose down -v && docker compose up --build`

---

## Run locally (without Docker)

Backend (needs .NET 8 SDK + PostgreSQL):
```bash
cd TenderDocs/TenderDocs-Backend
export ConnectionStrings__Default=
export Jwt__Secret=
export Encryption__Key=
dotnet run --project src/TenderDocs.Api      # serves on http://localhost:8080
```

Frontend (needs Node 20+):
```bash
cd TenderDocs/TenderDocs-Frontend/frontend
npm install
npm run dev                                   # http://localhost:5173, proxies /api -> :8080
```

---

## How to deploy

1. Supply production secrets via environment variables (don't commit them).
2. On the server: `cd TenderDocs-Backend && docker compose up -d --build`.
3. Front the nginx gateway with TLS (platform load balancer or a TLS-terminating proxy) and map
   `GATEWAY_PORT` to 80/443.
4. For production data, point `ConnectionStrings__Default` at managed PostgreSQL and persist the
   `storage` volume. Optionally connect Google Drive via `/api/google-drive/connect`.

---

## What was integrated

- **Frontend:** added `src/config/api.ts` + `src/services/api/*` (Auth, Documents, Projects,
  Folders, Organize, Notifications, Storage, Search) and `src/services/live.ts`; switched the
  service composition point from mock to live; wired real document upload and real "Download ZIP";
  added a Dockerfile + SPA nginx + dev proxy. Components and their contracts are unchanged.
- **Backend:** added `/api/organize`, `/api/storage`, `/api/users` controllers, a project
  document-set endpoint (`PUT /api/projects/{id}/documents`) and project summaries
  (`GET /api/projects/summary`), plus an idempotent demo-data seeder run on startup.
- **Infra:** single nginx gateway (gzip, WebSocket upgrade, asset caching), Compose pointed at the
  external frontend folder, ready-to-run `.env` with the provided secrets.
