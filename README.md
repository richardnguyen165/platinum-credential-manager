# Platinum Credential Manager

A full-stack application for securely storing and managing user credentials. It consists of a .NET 10 ASP.NET Core REST API backend, a Vue 3 single-page application frontend, and Keycloak for authentication.

## Quick Start

New to this repo? Here's the fastest path from clone to a logged-in app. Each step links to the fuller explanation further down.

1. **Clone it** and install the [Prerequisites](#prerequisites) below (.NET SDK, Node.js, Docker Desktop).
2. **Start Docker Desktop**, then from the repo root run `docker compose up -d` to bring up Keycloak. See [step 1](#1-keycloak-identity-provider).
3. **Start the API**: `cd PlatinumCredentialManager.Api && dotnet run`. `dotnet run` restores NuGet packages and applies database migrations automatically — no separate install step needed. See [step 2](#2-backend-api).
4. **Start the frontend**: `cd PlatinumCredentialManager.Client && npm install && npm run dev`. See [step 3](#3-frontend).
5. Visit `http://localhost:5173` — you'll be redirected to a Keycloak login page. Since no users exist yet, first go create one: see [Authentication (Keycloak)](#authentication-keycloak).
6. Log in with that user and the app loads.

That's the whole stack running. Details, troubleshooting, and the reasoning behind each piece are below.

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ |
| [Node.js](https://nodejs.org/) | 20.19+ or 22.12+ |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Any recent version (runs Keycloak) |

## Project Structure

```
platinum-credential-manager/
├── PlatinumCredentialManager.Api/     # ASP.NET Core backend
├── PlatinumCredentialManager.Client/  # Vue 3 + Vite frontend
├── keycloak/platinum-realm.json       # Keycloak realm export (auto-imported on startup)
└── docker-compose.yml                 # Keycloak container definition
```

## Getting Started

Three services must be running for the application to work: Keycloak, the API, and the frontend. Start each in its own terminal, in this order.

### 1. Keycloak (identity provider)

```bash
docker compose up -d
```

Starts Keycloak at `http://localhost:8080` and auto-imports the `platinum` realm (clients `platinum-vue` and `platinum-api`) from `keycloak/platinum-realm.json`.

- Admin console: `http://localhost:8080` → login `admin` / `admin`
- No users are seeded by the realm import — see [Authentication (Keycloak)](#authentication-keycloak) below to create one.
- Check status any time with `docker compose ps`; view logs with `docker compose logs -f keycloak`.
- Stop with `docker compose down` (add `-v` only if you intentionally want to wipe the realm/user data).

### 2. Backend (API)

```bash
cd PlatinumCredentialManager.Api
dotnet build
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5142`
- HTTPS: `https://localhost:7032`

On startup it applies EF Core migrations and validates JWTs issued by the `platinum` realm (see `Program.cs` — `Authority`/`Audience` must point at Keycloak, so Keycloak has to be up first).

### 3. Frontend

```bash
cd PlatinumCredentialManager.Client
npm install
npm run dev
```

The dev server will start and print the local URL (typically `http://localhost:5173`). Because the app calls `keycloak.init({ onLoad: 'login-required' })` in `main.js`, visiting this URL immediately redirects to the Keycloak login page — the Vue app won't render until you've authenticated.

## Authentication (Keycloak)

The app uses Keycloak (realm `platinum`) for login via OAuth2 Authorization Code + PKCE — the browser is redirected to Keycloak's hosted login page, never handles raw credentials, and gets back a JWT that the API validates.

**Creating a test user** (the realm import doesn't seed any):

1. Admin console (`http://localhost:8080`) → switch realm to `platinum` → **Users** → **Add user**.
2. Fill in username, email, first/last name (required — an incomplete profile triggers Keycloak's "Update Account Information" prompt on first login) → **Create**.
3. **Credentials** tab → **Set password** → enter a password → toggle **Temporary** to **Off** → **Save**.

Log in at `http://localhost:5173` with that username/password.

> The `platinum-vue` client has `directAccessGrantsEnabled: false` by design — the frontend must go through the redirect flow rather than posting credentials straight to Keycloak's token endpoint.

## Exploring the API (Swagger UI)

With the API running, you can browse and test every endpoint interactively — no need to hand-write requests.

| Page | URL | Purpose |
|------|-----|---------|
| Swagger UI | `http://localhost:5142/swagger` | Interactive UI: expand an endpoint, click **Try it out**, fill the fields, **Execute** |
| OpenAPI document | `http://localhost:5142/openapi/v1.json` | Raw JSON spec the UI is generated from |

Both are only served in the **Development** environment (they're wrapped in `if (app.Environment.IsDevelopment())` in `Program.cs`).

How it's wired:
- `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()` / `MapOpenApi()`) generates the OpenAPI document. .NET 10 emits OpenAPI **3.1** by default, but the document is pinned to **3.0** (`options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0`) because the bundled Swagger UI mishandles 3.1 — path parameters render as `integer | string` and validation wrongly reports *"Required field is not provided"* even when a value is entered.
- `Swashbuckle.AspNetCore.SwaggerUI` (`UseSwaggerUI(...)`) serves the UI and points it at `/openapi/v1.json`.

> Tip: after changing endpoints, restart the API (the document is generated at startup) and hard-refresh the Swagger page (Ctrl+F5) so the browser doesn't show a cached spec.

## Resetting the Database

The API uses a SQLite database stored in a single file, `PlatinumCredentialManager.db`, inside the `PlatinumCredentialManager.Api/` folder (see the `CredsStore` connection string in `appsettings.json`).

To wipe it and start fresh with the seed data, **delete the database file and run the app**. On startup the API applies migrations (`app.MigrateDb()`) and re-seeds the categories and credentials (the seeder only runs when the credentials table is empty, so a fresh database always gets seeded).

**PowerShell (Windows):**

```powershell
cd PlatinumCredentialManager.Api
Remove-Item PlatinumCredentialManager.db* -Force   # the * also clears the -wal / -shm files
dotnet run
```

**Bash (macOS / Linux):**

```bash
cd PlatinumCredentialManager.Api
rm -f PlatinumCredentialManager.db*
dotnet run
```

Notes:
- Stop the running API first — the file is locked while the app is using it.
- The `*` matters: SQLite may leave `PlatinumCredentialManager.db-wal` and `-shm` side files. Deleting only the `.db` can leave stale data behind.
- Alternatively, if you have the EF Core tools installed (`dotnet tool install --global dotnet-ef`), you can drop the database without deleting the file manually: `dotnet ef database drop --force`. The schema is still rebuilt on the next `dotnet run`.

## Tech Stack

- **Backend:** ASP.NET Core (.NET 10), C#
- **Frontend:** Vue 3, Vite, Axios
- **Identity:** Keycloak 26.2 (OAuth2 / OpenID Connect)
