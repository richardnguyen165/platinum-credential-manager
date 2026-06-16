# Platinum Credential Manager

A full-stack application for securely storing and managing user credentials. It consists of a .NET 10 ASP.NET Core REST API backend and a Vue 3 single-page application frontend.

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ |
| [Node.js](https://nodejs.org/) | 20.19+ or 22.12+ |

## Project Structure

```
platinum-credential-manager/
├── PlatinumCredentialManager.Api/     # ASP.NET Core backend
└── PlatinumCredentialManager.Client/  # Vue 3 + Vite frontend
```

## Getting Started

Both services must be running for the application to work. Start them in separate terminals.

### Backend (API)

```bash
cd PlatinumCredentialManager.Api
dotnet build
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5142`
- HTTPS: `https://localhost:7032`

### Frontend

```bash
cd PlatinumCredentialManager.Client
npm install
npm run dev
```

The dev server will start and print the local URL (typically `http://localhost:5173`).

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
