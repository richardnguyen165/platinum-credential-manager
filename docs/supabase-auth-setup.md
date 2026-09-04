# Switch auth from Keycloak to Supabase

## Why

Superseding [keycloak-frontend-setup.md](keycloak-frontend-setup.md): decided against Keycloak
for a portfolio project meant for other people to actually log into. The two deciding factors:

- **A custom login/sign-up form is a first-class, supported use of Supabase's API**
  (`signInWithPassword` / `signUp` are meant to be called from your own UI). The equivalent on
  Keycloak (Direct Access Grants) is a flow Keycloak itself discourages.
- **No IdP server to self-host.** Keycloak is a stateful Java service someone has to run 24/7 for
  a live demo. Supabase is a managed service (generous free tier) — nothing extra to deploy.

Trade-off accepted: this is a weaker "I integrated enterprise IAM/OIDC" portfolio signal than
Keycloak, and social-login-only parts of Supabase Auth still use OIDC under the hood, but the
plain email/password path does not.

## Current state (as of this plan)

- **Client**: `keycloak-js` in `package.json`; `src/config/keycloak.js` constructs the adapter;
  `src/main.js` calls `keycloak.init({ onLoad: 'check-sso', ... })` before mounting; `src/composables/useAuth.js`
  wraps `keycloak.login()` / `keycloak.logout()` and keeps a `reactive` `authState` in sync via
  `keycloak.onAuthSuccess` / `onAuthLogout` / `onTokenExpired`; `src/views/LogIn.vue` and
  `SignUp.vue` are empty placeholder templates; router already has `/`, `/login`, `/signup`.
- **API**: [Program.cs](../PlatinumCredentialManager.Api/Program.cs) has JWT bearer auth already
  correctly pointed at Keycloak (`Authority = http://localhost:8080/realms/platinum`,
  `Audience = platinum-api`), and CORS already allows `http://localhost:5173`.
  [`CredentialEndpoints`](../PlatinumCredentialManager.Api/Endpoints/CredentialEndpoints.cs) and
  `CategoryEndpoints` both call `.RequireAuthorization()` but **do not filter by user at all** —
  every authenticated caller currently sees and can edit every row in the SQLite database.
  [`Models/User.cs`](../PlatinumCredentialManager.Api/Models/User.cs) exists
  (`Id`, `KeycloakId` — "the sub claim from the JWT", `Categories`) but is **not wired into
  `CredsStoreContext`** (no `DbSet<User>`, no migration) and
  [`UserEndpoints.MapUserEndpoints`](../PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs)
  is an empty stub, never called from `Program.cs`. `Category` has no `UserId` foreign key either.
- **Infra**: `docker-compose.yml` runs only the Keycloak container + imports
  `keycloak/platinum-realm.json`. The API uses SQLite (`PlatinumCredentialManager.db`) via EF Core.

Because the per-user data model was never finished, this is a good time to build it correctly
against Supabase's user ids instead of half-finishing it against Keycloak's.

## Supabase project setup (one-time, no code)

1. Create a free project at supabase.com.
2. **Authentication → Providers → Email**: leave enabled. For a low-friction demo, consider
   turning off "Confirm email" (Authentication → Settings) so sign-up logs the user in
   immediately instead of waiting on a verification email.
3. **Project Settings → API**: copy the **Project URL** and **anon/public key** — both are safe
   to ship in the frontend bundle by design (Supabase's row-level security, not secrecy, is what
   protects data).
4. **Project Settings → API → JWT Settings**: note whether the project signs tokens with the
   legacy shared secret (HS256) or the newer per-project JWKS (asymmetric). This decides one
   branch in the API config below — check this before wiring up the backend.

## Client changes (`PlatinumCredentialManager.Client`)

| File | Change |
|------|--------|
| `package.json` | Remove `keycloak-js`; add `@supabase/supabase-js`. |
| `src/config/keycloak.js` | **Delete.** Replace with `src/config/supabase.js`: `export const supabase = createClient(import.meta.env.VITE_SUPABASE_URL, import.meta.env.VITE_SUPABASE_ANON_KEY)`. |
| `public/silent-check-sso.html` | **Delete** (Keycloak-specific, not used by Supabase). |
| `src/main.js` | Remove the `keycloak.init({...}).then(() => app.mount(...))` wrapping — Supabase's client doesn't need an init step before mount. Just `app.mount('#app')` directly; session restoration happens inside `useAuth.js` after mount. |
| `src/composables/useAuth.js` | Rewrite (see below). |
| `src/views/LogIn.vue` | Build the form: email + password fields, submit calls `login(email, password)` from `useAuth()`, show the thrown error message on failure. |
| `src/views/SignUp.vue` | Build the form: email + password (+ confirm password) fields, submit calls `signUp(email, password)` from `useAuth()`. |
| `src/api/http.js` | New Axios instance (this was never built yet). Request interceptor calls `supabase.auth.getSession()` (auto-refreshes if needed) and attaches `Authorization: Bearer <access_token>`. |
| `.env` | Add `VITE_SUPABASE_URL` / `VITE_SUPABASE_ANON_KEY`. |

### `useAuth.js` shape under Supabase

Supabase's JS SDK manages token storage and refresh itself (in `localStorage` by default), so
this file gets **simpler** than the Direct-Access-Grant version drafted earlier — no manual
`expiresAt` bookkeeping needed:

```js
import { reactive } from 'vue'
import { supabase } from '../config/supabase.js'

const authState = reactive({ authenticated: false, email: '' })

supabase.auth.onAuthStateChange((_event, session) => {
  authState.authenticated = !!session
  authState.email = session?.user?.email ?? ''
})

async function login(email, password) {
  const { error } = await supabase.auth.signInWithPassword({ email, password })
  if (error) throw error
}

async function signUp(email, password) {
  const { error } = await supabase.auth.signUp({ email, password })
  if (error) throw error
}

async function logout() {
  await supabase.auth.signOut()
}

export default function useAuth() {
  return { authState, login, signUp, logout }
}
```

`onAuthStateChange` is Supabase's equivalent of Keycloak's `onAuthSuccess`/`onAuthLogout` — it
fires on login, logout, token refresh, and on page load once Supabase restores an existing
session from `localStorage`, so `authState` self-populates without a separate init step.

Note the identity field is **email**, not username — Supabase's built-in `users` table is keyed
on email (or phone) by default. A separate "username" would need to be stored as user metadata or
in your own `User` table; not required to get login working.

## API changes (`PlatinumCredentialManager.Api`)

1. **`Program.cs` — swap the JWT bearer config:**
   ```csharp
   options.Authority = "https://<project-ref>.supabase.co/auth/v1";
   options.Audience = "authenticated";
   options.RequireHttpsMetadata = true; // Supabase is real HTTPS, unlike local Keycloak
   ```
   If the project uses the **legacy HS256 shared secret** (checked in Supabase setup step 4
   above) instead of JWKS, `Authority`-based discovery won't work — instead set
   `TokenValidationParameters.IssuerSigningKey` directly from a `SUPABASE_JWT_SECRET` env var
   (loaded via the existing `DotNetEnv.Env.Load()`) and drop `Authority`.

2. **Rename `User.KeycloakId`** → something provider-neutral, e.g. `SupabaseUserId` or `AuthId`.
   It still stores the same thing conceptually: the `sub` claim off the validated JWT — Supabase
   JWTs carry `sub` too, just a Supabase-issued UUID instead of a Keycloak one.

3. **Wire `User` into the data model** (currently disconnected — see Current State above):
   - Add `DbSet<User> Users` to `CredsStoreContext`.
   - Add a `UserId` foreign key to `Category` (credentials already hang off `Category`, so this
     scopes both in one FK).
   - New EF Core migration for both.

4. **Add user-scoping to every endpoint that touches `Category`/`Credential`.** This is the
   critical piece for a multi-user live demo — right now every request returns *all* rows,
   regardless of who's asking. In each handler in `CredentialEndpoints.cs` / `CategoryEndpoints.cs`,
   read the caller's id off `HttpContext.User.FindFirst("sub")?.Value`, look up (or create, on
   first sight) the matching local `User` row, and filter/attach every query to that `UserId`.

5. **Flesh out `UserEndpoints.MapUserEndpoints`** (currently an empty stub, never called from
   `Program.cs`) — or fold the "find-or-create local User row for this `sub`" logic into a small
   piece of middleware/filter that runs once per authenticated request instead. Either way, this
   needs to exist before step 4 can work.

6. **CORS** stays as-is for local dev (`http://localhost:5173`). Add the real deployed frontend
   origin once you pick a host — out of scope for this doc.

## Infra cleanup

- `docker-compose.yml`: remove the `keycloak` service and `keycloak_data` volume entirely —
  nothing left to self-host for auth.
- Delete `keycloak/platinum-realm.json` (or leave it for reference; it's no longer imported by
  anything once the compose service is gone).

## Verification

1. `npm install` in the client picks up `@supabase/supabase-js` and drops `keycloak-js`.
2. Sign up a test user via `SignUp.vue` → confirm a row appears in Supabase's Authentication →
   Users table.
3. Log in via `LogIn.vue` → `authState.authenticated` flips true, app shows the logged-in view.
4. Devtools → Application → Local Storage: confirm Supabase stored a session token (proves
   session persistence works without any custom code).
5. Issue a request through `src/api/http.js` to `/category` → confirm `Authorization: Bearer …`
   is attached and the API returns **only that user's** categories (proves step 4 of the API
   changes above actually scopes data, not just authenticates it).
6. Log in as a second test user → confirm they see an empty/separate category list, not the first
   user's data.
