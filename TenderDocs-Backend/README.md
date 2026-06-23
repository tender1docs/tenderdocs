# TenderDocs — Backend

ASP.NET Core 8 Web API for the existing **TenderDocs** tender-document management app.
Built with Clean Architecture, CQRS (MediatR), EF Core + PostgreSQL, JWT auth, a pluggable
storage abstraction (Local / Google Drive / future S3), and Docker.

This repository contains **only** the backend, database, infrastructure, and API contracts.
The React frontend is treated as complete and is **not** included or modified here — you point
the compose stack at your existing frontend folder (see [Frontend](#frontend)).

---

## Architecture

```
src/
  TenderDocs.Domain          Entities, enums, IStorageProvider — no dependencies
  TenderDocs.Application      CQRS handlers (MediatR), DTOs, validation, interfaces
  TenderDocs.Infrastructure   EF Core, migrations, storage providers, identity, services
  TenderDocs.Api              Controllers, middleware, Program.cs, Swagger
```

Dependency direction: `Domain ← Application ← Infrastructure ← Api`.

- **CQRS / MediatR** — every use case is a `Command` or `Query` with its own handler.
  Cross-cutting validation and logging run as MediatR pipeline behaviors.
- **Storage abstraction** — `IStorageProvider` (UploadFile, DownloadFile, DeleteFile,
  MoveFile, CreateFolder, GetFolderTree, GenerateProjectZip). `IStorageProviderFactory`
  resolves the active provider per organization. `LocalStorageProvider` is the default;
  `GoogleDriveStorageProvider` activates once Drive is connected; `S3StorageProvider` is a stub.
- **Folders** — unlimited nesting via adjacency list (`ParentFolderId`) **plus** a
  materialized path (`MaterializedPath` + `Depth`) indexed for fast subtree retrieval.
- **Soft deletes** — `IsDeleted` / `DeletedAt` with EF global query filters.
- **Auth** — JWT access tokens + rotating refresh tokens, Google OAuth login, and
  role-based access (`Admin`, `Manager`, `Viewer`).

---

## Quick start

```bash
# 1. Configure
cp .env.example .env
#    edit .env and fill in the REQUIRED values (see "Required configuration" below)

# 2. Point the stack at your existing frontend (optional — see Frontend section)
#    set FRONTEND_CONTEXT in .env to the folder containing the frontend Dockerfile

# 3. Build and run
docker compose up --build
```

Services come up behind nginx:

| URL                             | What                    |
| ------------------------------- | ----------------------- |
| `http://localhost:8080/`        | Frontend (via nginx)    |
| `http://localhost:8080/api`     | API                     |
| `http://localhost:8080/swagger` | Swagger UI (if enabled) |
| `http://localhost:8080/health`  | Health check            |

`GATEWAY_PORT` (default `8080`) controls the published nginx port.

The API **applies EF Core migrations automatically on startup**, so the schema is created
on first run against a fresh PostgreSQL volume. The first user to register becomes the
organization **Admin**.

### Running the API without Docker (local dev)

```bash
dotnet restore
dotnet run --project src/TenderDocs.Api
```

`appsettings.Development.json` ships with a localhost PostgreSQL connection string and
dev-only JWT/encryption secrets, plus Swagger enabled — so it runs immediately against a
local Postgres without any secrets of your own. **Do not use those dev secrets in production.**

---

## Frontend

The frontend is **not** part of this repo and is not modified. The `frontend` service in
`docker-compose.yml` builds from `FRONTEND_CONTEXT` (default `./frontend`), which must point
at your existing frontend folder containing its own Dockerfile. It is served at `/` and
proxies API calls to `/api` (`VITE_API_BASE_URL=/api`).

If you keep the frontend in a different location, set `FRONTEND_CONTEXT` in `.env`
to that path. If you don't want compose to build the frontend at all, remove or comment
out the `frontend` service in `docker-compose.yml`.

---

## API surface

All routes are under `/api`. Auth endpoints are anonymous; everything else requires a
Bearer token. Upload/connect operations are restricted by role.

**Auth** — `POST /api/auth/register`, `POST /api/auth/login`,
`POST /api/auth/refresh`, `POST /api/auth/google`, `GET /api/auth/me`

**Documents** — `GET /api/documents` (search + filters), `GET /api/documents/{id}`,
`POST /api/documents` (multipart upload — Admin/Manager), `GET /api/documents/{id}/download`,
`PUT /api/documents/{id}`, `DELETE /api/documents/{id}`

**Projects** — `GET /api/projects`, `GET /api/projects/{id}`, `POST /api/projects`,
`DELETE /api/projects/{id}`, `POST /api/projects/{id}/documents`,
`DELETE /api/projects/{id}/documents/{documentId}`, `GET /api/projects/{id}/zip` (streamed ZIP)

**Folders** — `GET /api/folders/tree`, `POST /api/folders`,
`POST /api/folders/{id}/move`, `DELETE /api/folders/{id}`

**Notifications** — `GET /api/notifications`, `POST /api/notifications/{id}/read`,
`POST /api/notifications/read-all`

**Google Drive** — `GET /api/google-drive/status`,
`POST /api/google-drive/connect` (Admin), `POST /api/google-drive/disconnect` (Admin)

**Search** — `GET /api/search?q=...`

**Dashboard** — `GET /api/dashboard/stats`

### Project ZIP packaging

`GET /api/projects/{id}/zip` streams a ZIP grouped by document type:

```
Project Name/
  GST/
  PAN/
  Financial/    (ITR, Balance Sheet, MSME)
  Technical/    (Tender forms)
  Others/
```

---

## Required configuration

Copy `.env.example` to `.env` and fill these in. `appsettings.template.json` shows the same
values in `appsettings` form if you configure the API outside Docker.

**Required to run:**

| Value               | `.env` variable     | Notes                                       |
| ------------------- | ------------------- | ------------------------------------------- |
| PostgreSQL database | `POSTGRES_DB`       | DB name                                     |
| PostgreSQL user     | `POSTGRES_USER`     |                                             |
| PostgreSQL password | `POSTGRES_PASSWORD` | use a strong value                          |
| JWT secret          | `JWT_SECRET`        | **at least 32 characters**, random          |
| Encryption key      | `ENCRYPTION_KEY`    | encrypts stored storage credentials at rest |

The API composes its PostgreSQL connection string from the `POSTGRES_*` values. To use an
external database instead, set the connection string directly in `docker-compose.yml`.

**Optional — Google Drive / Google OAuth login** (only needed when you connect Drive or
enable Google sign-in):

| Value                  | `.env` variable          |
| ---------------------- | ------------------------ |
| Google Client ID       | `GOOGLE_CLIENT_ID`       |
| Google Client Secret   | `GOOGLE_CLIENT_SECRET`   |
| Google Redirect URI    | `GOOGLE_REDIRECT_URI`    |
| Google Drive Folder ID | `GOOGLE_DRIVE_FOLDER_ID` |

Drive credentials submitted at runtime via `POST /api/google-drive/connect` are AES-encrypted
with `ENCRYPTION_KEY` before being stored. Connecting Drive switches the org out of demo mode.

**Optional — SMTP** (email notifications; no-op if left blank):

`SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM`

---

## Notes

- No secrets are hardcoded. Production secrets come from `.env` / environment variables.
- Swagger is on automatically in Development; in other environments set `SWAGGER_ENABLED=true`.
- The migration is committed under
  `src/TenderDocs.Infrastructure/Persistence/Migrations`; the schema uses snake_case table
  names with PascalCase columns, `uuid` primary keys, and `jsonb` for audit details.

Wired into UploadDocument.cs before storage, registered in DI, configurable via the new Compression section in appsettings.json (Level: Light / Balanced / Maximum).
