# TenderDocs — Backend

ASP.NET Core 8 Web API for TenderDocs. Clean Architecture (Domain / Application / Infrastructure /
Api) with CQRS via MediatR, EF Core + PostgreSQL, JWT auth, Swagger, and a pluggable storage
provider (Local in demo mode, Google Drive / S3 optional). It ships with a Docker setup that also
builds and serves the frontend behind a single nginx gateway.

---

## Installation

### Prerequisites
- Docker & Docker Compose (recommended), **or**
- .NET 8 SDK + PostgreSQL 16 for running outside Docker

### Quick start (Docker — recommended)
```bash
cd TenderDocs-Backend
docker compose up --build
```
This starts PostgreSQL, the API, the frontend, and nginx. Open **http://localhost:8080**.

A ready-to-run `.env` with the project secrets is already included.

---

## Docker Setup

`docker compose up --build` starts four services:

| Service | Role | Port |
| --- | --- | --- |
| `postgres` | PostgreSQL 16 database | internal `5432` |
| `api` | ASP.NET Core API (applies migrations + seeds on startup) | internal `8080` |
| `frontend` | React SPA built and served by nginx | internal `80` |
| `nginx` | Gateway/reverse proxy — single public URL | **`8080`** (host) |

Routing through the gateway:
- `/` → frontend SPA
- `/api` → API
- `/swagger` → Swagger UI
- `/health` → health checks

The frontend build context points at the sibling folder via `FRONTEND_CONTEXT`
(default `../TenderDocs-Frontend/frontend`).

Useful commands:
```bash
docker compose up --build          # build + run everything
docker compose up -d --build       # detached
docker compose logs -f api         # follow API logs
docker compose down                # stop
docker compose down -v             # stop + wipe DB/storage volumes (fresh seed next run)
```

---

## PostgreSQL Setup

In Docker, PostgreSQL runs as the `postgres` service with a persistent `pgdata` volume; no manual
setup is needed. The API composes its connection string from the `POSTGRES_*` variables:

```
Host=postgres;Port=5432;Database=tenderdocs;Username=tenderdocs;Password=TenderDocs@2025
```

Running the API outside Docker against a local PostgreSQL:
```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=tenderdocs;Username=tenderdocs;Password=TenderDocs@2025"
dotnet run --project src/TenderDocs.Api
```

---

## Environment Variables

Set in `TenderDocs-Backend/.env` (read by Docker Compose). See `.env.example` for the template.

| Variable | Required | Default | Notes |
| --- | --- | --- | --- |
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | ✅ | tenderdocs / tenderdocs / — | Database + API connection string |
| `JWT_SECRET` | ✅ | — | ≥ 32 chars, signs access tokens |
| `JWT_ISSUER` / `JWT_AUDIENCE` | ✅ | TenderDocs / TenderDocsClient | Token issuer/audience |
| `ENCRYPTION_KEY` | ✅ | — | Encrypts stored storage credentials |
| `GATEWAY_PORT` | — | 8080 | Host port for nginx |
| `ASPNETCORE_ENVIRONMENT` | — | Production | Standard ASP.NET env |
| `SWAGGER_ENABLED` | — | true | Expose Swagger in non-dev |
| `SEED_ENABLED` | — | true | Seed demo data on first run |
| `FRONTEND_CONTEXT` | — | ../TenderDocs-Frontend/frontend | Frontend build context |
| `VITE_API_BASE_URL` | — | /api | Passed to the frontend build |
| `GOOGLE_*` | optional | empty | Google Drive / Google login |
| `SMTP_*` | optional | empty | Email notifications |

---

## Migrations

EF Core migrations are **applied automatically on startup** (see `Program.cs` →
`ApplyMigrationsAsync`). No manual step is needed for Docker.

Working with migrations manually (requires the EF tools: `dotnet tool install --global dotnet-ef`):
```bash
# add a migration
dotnet ef migrations add <Name> \
  --project src/TenderDocs.Infrastructure --startup-project src/TenderDocs.Api

# apply migrations to the database
dotnet ef database update \
  --project src/TenderDocs.Infrastructure --startup-project src/TenderDocs.Api
```

### Seed data
On first run (when no users exist) the API seeds a demo organization, an Admin user, a folder
tree, sample documents (with real files written to storage so download/ZIP work), projects,
assignments, and notifications. Disable with `SEED_ENABLED=false`.

**Seeded admin login:** `admin@tenderdocs.io` / `Admin@12345`
(the frontend auto-logs-in with these).

---

## Swagger

Enabled by default (`SWAGGER_ENABLED=true`) and in Development. Through the gateway:

```
http://localhost:8080/swagger
```

Click **Authorize** and paste a JWT access token (without the `Bearer ` prefix) to call secured
endpoints. Get a token from `POST /api/auth/login`.

---

## Deployment

1. Provide production secrets via environment variables (never commit real secrets).
2. Build and run with Compose on the target host:
   ```bash
   docker compose up -d --build
   ```
3. Put a TLS terminator (or your platform's load balancer) in front of the nginx gateway and map
   `GATEWAY_PORT` to 80/443.
4. Use managed PostgreSQL in production by overriding `ConnectionStrings__Default` on the `api`
   service and dropping the `postgres` service if desired.
5. Persist the `storage` volume (uploaded documents) and `pgdata` (database).

---

## Health Checks

- API health endpoint: `GET /health` (also `http://localhost:8080/health` via the gateway).
  Reports PostgreSQL connectivity.
- Compose health checks are configured for `postgres` (pg_isready) and `api` (curl `/health`);
  the API waits for a healthy database before starting.

---

## Troubleshooting

| Symptom | Likely cause / fix |
| --- | --- |
| `docker compose up` exits building frontend | Ensure the sibling `TenderDocs-Frontend/frontend` exists, or set `FRONTEND_CONTEXT` to its path. |
| API restarts / can't reach DB | Wait for `postgres` to become healthy; check `JWT_SECRET` is ≥ 32 chars and `POSTGRES_*` match. |
| 401 on every API call | The frontend auto-logs-in to the seeded admin; if you set `SEED_ENABLED=false`, it registers a fresh workspace instead. Verify `/api/auth/login` works in Swagger. |
| Empty dashboard / no data | Seed didn't run because users already exist. Reset with `docker compose down -v` then `up --build`. |
| Swagger 404 | Confirm `SWAGGER_ENABLED=true`; browse `/swagger/` (trailing slash) through the gateway. |
| Uploads fail with 413 | Increase `client_max_body_size` in `nginx/nginx.conf` (default 200m). |
| Port 8080 in use | Change `GATEWAY_PORT` in `.env`. |
| Lost uploaded files after restart | Don't run `docker compose down -v` unless you intend to wipe the `storage`/`pgdata` volumes. |

---

## Project Structure

```
TenderDocs-Backend/
├── docker-compose.yml      # postgres + api + frontend + nginx
├── Dockerfile              # API image
├── .env / .env.example     # secrets + config
├── nginx/nginx.conf        # gateway: / -> frontend, /api,/swagger,/health -> api
└── src/
    ├── TenderDocs.Domain/         # entities, enums, interfaces
    ├── TenderDocs.Application/     # CQRS features (MediatR), DTOs, validation
    │   └── Features/{Auth,Documents,Projects,Folders,Assignments,
    │                  Notifications,Search,Dashboard,Users,GoogleDrive}
    ├── TenderDocs.Infrastructure/  # EF Core, persistence, storage, identity, DbSeeder
    └── TenderDocs.Api/             # controllers, middleware, Program.cs
        └── Controllers/{Auth,Documents,Projects,Folders,Organize,
                          Notifications,Storage,Search,Users,Dashboard,GoogleDrive}
```
