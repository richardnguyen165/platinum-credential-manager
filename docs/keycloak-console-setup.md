# Configure Keycloak from the admin console (hosted login + registration)

> Click-by-click setup of the `platinum` realm through the Keycloak web UI, for the
> **hosted-pages** auth flow (`keycloak-js` redirects to Keycloak's own login/register screens).
> Companion to [keycloak-frontend-setup.md](keycloak-frontend-setup.md) (Vue wiring) and
> [api-user-provisioning.md](api-user-provisioning.md) (local `User` row after login).
> The custom in-app form alternative is [keycloak-signup-setup.md](keycloak-signup-setup.md) (deferred).

Repo values used below: realm `platinum`, SPA client `platinum-vue`, API audience `platinum-api`,
Keycloak `:8080`, Vue `:5173`, API `:5142`.

## 0. Start Keycloak

```
docker compose up -d
```

[docker-compose.yml](docker-compose.yml) runs Keycloak 26.2 and imports
[keycloak/platinum-realm.json](keycloak/platinum-realm.json) **only if the `platinum` realm does
not already exist** in the `keycloak_data` volume.

## 1. Open the admin console

`http://localhost:8080/admin/` → sign in `admin` / `admin` (from `KC_BOOTSTRAP_ADMIN_USERNAME` /
`KC_BOOTSTRAP_ADMIN_PASSWORD` in the compose file).

Top-left realm dropdown → switch from **master** to **platinum**. Do all the steps below in
`platinum`; never configure the app in `master`.

## 2. Enable self-registration

**Realm settings → Login** tab:

| Setting | Value | Why |
|---------|-------|-----|
| User registration | **On** | Adds the "Register" link to the hosted login page (`keycloak.register()` targets it) |
| Forgot password | On (optional) | Free "Forgot password?" link |
| Verify email | **Off** | No SMTP server is configured; leaving this on blocks new logins |
| Login with email | On (already) | — |
| Email as username | On (already) | Register form shows a single email field, no separate username |

## 3. Review the `platinum-vue` client

**Clients → `platinum-vue` → Settings**:

| Field | Value |
|-------|-------|
| Valid redirect URIs | `http://localhost:5173/*` |
| Valid post logout redirect URIs | `http://localhost:5173/*` (or `+` to reuse redirect URIs) |
| Web origins | `http://localhost:5173` (or `+`) |
| Client authentication | **Off** (public SPA) |
| Standard flow | **On** (Authorization Code — the redirect flow) |
| Direct access grants | Off (not used by the hosted flow) |

Without a **post logout redirect URI**, `keycloak.logout({ redirectUri })` is rejected by
Keycloak 26.

**Advanced** tab → **Proof Key for Code Exchange Code Challenge Method** → `S256` (matches
`pkceMethod: 'S256'` in [main.js](PlatinumCredentialManager.Client/src/main.js)).

## 4. Add the API audience mapper (fixes 401 on every API call)

Access tokens issued to `platinum-vue` do **not** carry `aud: platinum-api`, so the .NET API
(`options.Audience = "platinum-api"` in [Program.cs](PlatinumCredentialManager.Api/Program.cs))
rejects them all with `401`.

**Clients → `platinum-vue` → Client scopes → `platinum-vue-dedicated` → Mappers →
Add mapper → By configuration → Audience**:

| Field | Value |
|-------|-------|
| Name | `platinum-api-audience` |
| Included Client Audience | `platinum-api` |
| Add to access token | **On** |

Save.

## 5. Create a login user

Either self-register (once step 2 is done) at
`http://localhost:5173` → "Sign Up", **or** in the console:

**Users → Add user** → Username + Email, **Email verified: On** → Create →
**Credentials** tab → **Set password** → set password, **Temporary: Off** → Save.

## 6. Verify

1. `http://localhost:8080/realms/platinum/.well-known/openid-configuration` → returns JSON.
2. Log in through the app, then in devtools console:
   ```js
   JSON.parse(atob(keycloak.token.split('.')[1]))
   ```
   - `iss` = `http://localhost:8080/realms/platinum`
   - `aud` contains `platinum-api`  ← step 4 worked
   - `preferred_username` is set  ← used by `useAuth.js`
3. A request to `/user/me` or `/category` returns `2xx`, not `401`.

## 7. Persist console changes back to the repo

Console edits are saved in the `keycloak_data` Docker volume, **not** written back to
[keycloak/platinum-realm.json](keycloak/platinum-realm.json).

| Action | Effect on your console changes |
|--------|-------------------------------|
| `docker compose restart` / `stop` / `down` then `up` | Kept (volume persists) |
| `docker compose down -v` | **Lost** — realm re-imported from the JSON |

To make the JSON match what you clicked:

- **Realm settings → Action (top-right) → Partial export** — pick clients / roles / groups, downloads JSON, or
- ```
  docker compose exec keycloak /opt/keycloak/bin/kc.sh export --dir /tmp/export --realm platinum --users skip
  docker compose cp keycloak:/tmp/export/platinum-realm.json ./keycloak/platinum-realm.json
  ```

Then diff and commit the parts you want as the reproducible baseline (registration flag, the
audience mapper, the post-logout URI).

## Reference documentation

| Step | Official doc |
|------|--------------|
| Console basics — realm, client, user | [Getting Started: Docker](https://www.keycloak.org/getting-started/getting-started-docker) |
| Everything console | [Server Administration Guide](https://www.keycloak.org/docs/latest/server_admin/index.html) |
| Step 2 — login settings / user registration | Server Admin Guide → *Configuring realms* → "Login settings" |
| Step 3 — client settings, PKCE | Server Admin Guide → [Creating an OIDC client](https://www.keycloak.org/docs/latest/server_admin/index.html#proc-creating-oidc-client_server_administration_guide) |
| Step 4 — audience mapper | Server Admin Guide → [Audience support](https://www.keycloak.org/docs/latest/server_admin/index.html#_audience); [Protocol mappers](https://www.keycloak.org/admin-api/protocol-mappers) |
| Step 6 — OIDC endpoints | [Securing Applications and Services Guide](https://www.keycloak.org/docs/latest/securing_apps/index.html) |
| Frontend adapter | [keycloak-js adapter](https://www.keycloak.org/securing-apps/javascript-adapter) |
| Version pinned to 26.2.5 | swap `latest` → `26.2.5` in `/docs/` URLs, or [documentation archive](https://www.keycloak.org/documentation-archive) |
