# Getting Keycloak working in Docker over HTTPS

What it took to get the app running end to end after Keycloak moved into Docker
and the frontend moved to HTTPS (`https://localhost:5173`). Use this as a
checklist when setting up another copy of the repo, such as the one at home.

Address map, for reference:

| Piece | Address |
|---|---|
| Vue frontend (Vite + mkcert) | `https://localhost:5173` |
| .NET API | `https://localhost:7032` |
| Keycloak (Docker) | `https://localhost:8443` |

## 1. Docker

1. Install Docker Desktop and start it. Wait until it says the engine is running.
2. **Fully restart VS Code** after installing. A terminal opened before the
   install doesn't have `docker` on its PATH, which gives
   `docker : The term 'docker' is not recognized`. Opening a new terminal tab
   isn't enough, because VS Code passes on the environment it started with.
3. Check it works: `docker --version` and `docker compose version`.

## 2. `.env` (not in git, so recreate it on every machine)

`docker-compose.yml` mounts the mkcert certificate and key into Keycloak using
two variables from `.env`, which is in the repo root and listed in `.gitignore`:

```
CERT_PATH="C:/Users/<your-user>/.vite-plugin-mkcert/cert.pem"
DEV_PATH="C:/Users/<your-user>/.vite-plugin-mkcert/dev.pem"
```

- **Use forward slashes.** In a double-quoted value, Compose treats a backslash
  as the start of an escape sequence, so `\richard` became a carriage return
  followed by `ichard`. The resulting error was
  `CreateFile C:\Users ichard.nguyen\... The filename, directory name, or volume label syntax is incorrect`.
  Single quotes or forward slashes both avoid this.
- **The username in the path is different on each machine.** Run the frontend
  once first (`npm run dev`) so `vite-plugin-mkcert` creates the cert files.
- Check the result with `docker compose config`, which prints the compose file
  with the variables filled in. Then run `docker compose up -d`.

## 3. Keycloak client `platinum-vue` (admin console changes)

In the admin console at `https://localhost:8443` (bootstrap admin
`admin` / `admin`), open the **platinum** realm, then
**Clients → platinum-vue → Settings**:

| Setting | Value(s) |
|---|---|
| Valid redirect URIs | `https://localhost:5173/*`, `https://localhost:7032/*` |
| Web origins | `https://localhost:5173`, `https://localhost:7032` |

Click **Save**.

> **These changes live only in the Docker volume (`keycloak_data`).**
> `--import-realm` reads `keycloak/platinum-realm.json` only when the realm
> doesn't exist yet. The realm file in git still lists only
> `https://localhost:7032`. On a fresh machine you can either:
> - redo the two settings above in the admin console, **or**
> - update `keycloak/platinum-realm.json` so the `platinum-vue` client contains
>   these values, then do a clean import:
>   `docker compose down -v` (`-v` deletes the volume, so all Keycloak data and
>   users are lost) and then `docker compose up -d`.

## 4. API (`Program.cs`, in git once committed)

Two addresses were left over from before the HTTPS and Docker switch:

| Setting | Old | New |
|---|---|---|
| CORS allowed origin | `http://localhost:5173` | `https://localhost:5173` |
| JWT `Authority` | `http://localhost:8080/realms/platinum` | `https://localhost:8443/realms/platinum` |

Restart the API after changing these. Configuration isn't hot-reloaded.

## Symptoms and causes

Each fix revealed the next error. If one of these shows up at home, this table
says which setting to check.

| Symptom | Cause | Fix |
|---|---|---|
| Blank page. Console: `Refused to display 'https://localhost:8443/' in a frame because it set 'X-Frame-Options' to 'sameorigin'` | The silent SSO check runs in a hidden iframe. Keycloak rejected the `redirect_uri` and showed an error page, which isn't allowed in a frame. `keycloak.init()` never resolves, so the app never mounts. | Add the frontend to **Valid redirect URIs**. Don't loosen `X-Frame-Options`: that only hides the error. |
| Tip: reading the hidden error | Copy the failing `auth?client_id=...` request URL from the Network tab and open it in its own tab. It showed `Invalid parameter: redirect_uri`. | |
| `POST .../openid-connect/token` returns `403 (Forbidden)` with no `Access-Control-Allow-Origin` header | The request's `Origin` (`https://localhost:5173`) isn't in the client's **Web origins** list. | Add the frontend origin to **Web origins**: scheme, host and port only, with no trailing slash or path. |
| `Access to XMLHttpRequest at 'https://localhost:7032/user/me' ... blocked by CORS policy` | The API's CORS policy still allowed `http://` instead of `https://`. | Update the API CORS origin and restart the API. |

The underlying lesson: an origin is **scheme + host + port**, and every
allow-list has to match it exactly. Switching to HTTPS changed the scheme, so
every list that still said `http://` broke.

- **Redirect URIs** control where Keycloak may send the browser.
- **Web origins** control which pages may read Keycloak's responses from
  JavaScript (CORS on the token endpoint).
- **The API's CORS policy** controls which pages may call the API.
