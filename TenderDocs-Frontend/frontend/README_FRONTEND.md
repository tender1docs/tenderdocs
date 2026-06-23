# TenderDocs — Frontend

React + TypeScript + Vite SPA for the TenderDocs tender-document manager. It talks to the
ASP.NET Core 8 backend through a single nginx gateway, so in both Docker and local dev the
API lives at the same origin under `/api`.

- **Stack:** React 18, TypeScript, Vite 5, Tailwind CSS, React Query, React Router, Framer Motion
- **Data layer:** typed API clients in `src/services/api` + live services in `src/services/live.ts`
  that implement the same `I*Service` contracts the screens already consume (no mock data at runtime)

---

## Installation

```bash
cd TenderDocs-Frontend/frontend
npm install
```

Requires Node.js 20+.

---

## Environment Variables

Configured via `.env` (a ready-to-run file is included). See `.env.example` for all options.

| Variable | Default | Purpose |
| --- | --- | --- |
| `VITE_API_BASE_URL` | `/api` | Base URL the SPA uses to reach the backend. Same-origin `/api` works through the nginx gateway (Docker) and the Vite dev proxy (local). Set an absolute URL (e.g. `http://localhost:8080/api`) to call a remote API directly. |
| `VITE_DEMO_EMAIL` | `admin@tenderdocs.io` | Auto-login account (matches the seeded admin). |
| `VITE_DEMO_PASSWORD` | `Admin@12345` | Auto-login password. |
| `VITE_PROXY_TARGET` | `http://localhost:8080` | Dev-only: where `npm run dev` proxies `/api`. |

> The app has no login screen by design — on first request it authenticates against the seeded
> admin and caches the JWT in `localStorage` (refreshing automatically on 401). To add a real login
> screen later, call `setAuthTokens()` from `src/config/api.ts`.

---

## Development

```bash
npm run dev
```

Runs Vite on `http://localhost:5173`. Requests to `/api`, `/swagger`, and `/health` are proxied
to the backend at `http://localhost:8080` (start the backend with `docker compose up` first).

---

## Production Build

```bash
npm run build      # tsc + vite build -> dist/
npm run preview    # preview the production bundle locally
```

Output is a static bundle in `dist/` with content-hashed assets.

---

## Docker Build

The frontend is built and served automatically by the root `docker compose up --build`
(run from `TenderDocs-Backend/`). To build the image on its own:

```bash
docker build -t tenderdocs-frontend .
docker run -p 8081:80 tenderdocs-frontend
```

The image is a multi-stage build: Node builds the SPA, then nginx serves `dist/` on port 80
with SPA history fallback, gzip, and long-lived asset caching. The gateway proxies `/` to it.

---

## Folder Structure

```
frontend/
├── Dockerfile              # multi-stage: node build -> nginx static serve
├── nginx.conf             # SPA fallback + gzip + asset caching (this container)
├── .env / .env.example    # VITE_API_BASE_URL etc.
├── index.html
├── vite.config.ts         # @ alias + dev proxy to the backend
└── src/
    ├── config/
    │   └── api.ts          # base URL, token storage, auto-login, fetch wrapper
    ├── services/
    │   ├── api/            # typed API clients (Auth, Documents, Projects, Folders,
    │   │                   #   Organize, Notifications, Storage, Search) + dtos.ts
    │   ├── live.ts         # I*Service impls mapping backend DTOs -> view types
    │   ├── index.ts        # composition point (exports live services)
    │   ├── store.ts        # mock services (kept for offline use; not wired)
    │   └── seed.ts         # mock seed data + requirement template
    ├── hooks/              # React Query hooks (useDocuments, useProjects, …)
    ├── pages/              # Dashboard, Documents, Projects, ProjectDetail, Organize, …
    ├── features/           # dashboard charts, documents, organize workspace
    ├── components/         # layout + UI primitives
    ├── layouts/            # AppLayout
    ├── routes/             # router
    └── types/              # view models (DocumentItem, ProjectItem, …)
```

---

## How the frontend talks to the backend

`src/services/api/*` issue HTTP calls; `src/services/live.ts` maps backend DTOs to the
frontend's view types and implements the `I*Service` contracts; React Query hooks call those
services. Because the contracts are unchanged, every screen runs on real backend data with no
component changes. The "Download ZIP" buttons stream the real archive from the API.
