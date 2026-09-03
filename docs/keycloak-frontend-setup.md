# Set up Keycloak on the Vue frontend

## Context

Keycloak is already provisioned for this repo (`docker-compose.yml` runs Keycloak 26.2 on
`:8080` and imports `keycloak/platinum-realm.json`), and `keycloak-js@26.2.4` is already in
`PlatinumCredentialManager.Client/package.json`. What is missing is the wiring in the Vue app:

- [src/config/keycloak.js](PlatinumCredentialManager.Client/src/config/keycloak.js) constructs the adapter but nothing ever calls `.init()`.
- [src/main.js](PlatinumCredentialManager.Client/src/main.js) imports it as a **default** import (`import keycloak from ...`) while the file only has a **named** export (`export const keycloak`), so the value is `undefined`; the app also mounts unconditionally.
- [src/App.vue](PlatinumCredentialManager.Client/src/App.vue) is a static "Log-In Page" placeholder with no auth logic.
- There is no Axios instance, so nothing attaches a token to the protected `/creds` and `/category` endpoints (both use `.RequireAuthorization()`).

Goal: initialise keycloak-js with a **silent-SSO + explicit Login button** flow (`check-sso`),
expose auth state to components, and send the access token (with auto-refresh) on API calls.

## Blocking issues this plan resolves or flags

1. **Export/import mismatch** — make `keycloak.js` `export default` so `main.js` and the new files can import the single shared instance.
2. **No realm users** — `platinum-realm.json` has an empty `users` set. Login is impossible until a user exists (create one in the admin console, or add a `users` block and re-import).
3. **Vite port must be 5173** — the `platinum-vue` client's redirect URIs are hardcoded to `http://localhost:5173/*` / web origin `http://localhost:5173`. Pin the dev server so a busy port doesn't silently break the redirect.
4. **`platinum-vue` has no `post.logout.redirect.uris`** — Keycloak 26 rejects a `redirectUri` on logout unless it is registered. Either add `post.logout.redirect.uris` = `+` to the client (admin console or realm JSON), or call `keycloak.logout()` with no `redirectUri`.
5. **Backend blockers (out of frontend scope, but you hit them on the first API call):** in [Program.cs](PlatinumCredentialManager.Api/Program.cs) `Authority` is `http://localhost:8080/realms/` (missing `platinum`), `Audience` is `"plantinum-api"` (typo), and CORS `WithOrigins("http://localhost:5142")` should be `http://localhost:5173`.

## Steps

### 1. Run Keycloak and create a login user

- From the repo root: `docker compose up -d`
- Verify: `http://localhost:8080/realms/platinum/.well-known/openid-configuration` returns JSON.
- `http://localhost:8080` → sign in `admin` / `admin` → switch to realm **platinum** → **Users** → **Add user** (set username + email, toggle *Email verified*) → **Credentials** → **Set password**, **Temporary = off**.
- Optional (persist across `docker compose down -v`): add a `users` array to `keycloak/platinum-realm.json` and re-import.

### 2. Fix `src/config/keycloak.js` (export + config)

Keep the same config values, switch to a default export, allow env overrides with the current
values as fallbacks (so it still works with no `.env`):

```js
import Keycloak from 'keycloak-js'

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'platinum',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'platinum-vue',
})

export default keycloak
```

### 3. Add `public/silent-check-sso.html` (required by `check-sso`)

Static file served verbatim from `public/`; no build step:

```html
<!DOCTYPE html>
<html><body><script>
  parent.postMessage(location.href, location.origin)
</script></body></html>
```

`http://localhost:5173/silent-check-sso.html` is covered by the client's `http://localhost:5173/*` redirect URI.

### 4. Initialise before mount in `src/main.js`

```js
import { createApp } from 'vue'
import App from './App.vue'
import keycloak from './config/keycloak.js'

keycloak
  .init({
    onLoad: 'check-sso',
    silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
    pkceMethod: 'S256',
    checkLoginIframe: false,
  })
  .then(() => createApp(App).mount('#app'))
```

- `check-sso` renders the app whether or not the user is logged in (no forced redirect).
- `pkceMethod: 'S256'` — explicit PKCE for the public `platinum-vue` client (the realm JSON does not force it server-side).
- `checkLoginIframe: false` — avoids third-party-cookie flakiness on localhost.

### 5. Expose auth state — `src/composables/useAuth.js`

Small reactive wrapper, no new dependency:

- `reactive({ authenticated: false, username: '' })`, seeded from `keycloak` after init.
- `login()` → `keycloak.login()`; `logout()` → `keycloak.logout()` (see blocking issue #4 re: `redirectUri`).
- Register `keycloak.onAuthSuccess`, `onAuthLogout`, and `onTokenExpired` (the last calls `keycloak.updateToken(30)`) to keep the state object current.
- Export a `useAuth()` that returns the state + `login` / `logout`.

### 6. Replace `src/App.vue` placeholder

Use `useAuth()`: when `!authenticated` show a **Log in** button calling `login()`; when
`authenticated` show `Hello, {{ username }}` and a **Log out** button calling `logout()`.

### 7. Token-aware API client — `src/api/http.js`

```js
import axios from 'axios'
import keycloak from '@/config/keycloak.js'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5142',
})

http.interceptors.request.use(async (config) => {
  if (keycloak.authenticated) {
    await keycloak.updateToken(30) // refresh if <30s of life left
    config.headers.Authorization = `Bearer ${keycloak.token}`
  }
  return config
})

http.interceptors.response.use(
  (r) => r,
  (error) => {
    if (error.response?.status === 401) keycloak.login()
    return Promise.reject(error)
  },
)

export default http
```

Use this instance for all `/creds` and `/category` requests.

### 8. (Optional) Pin the Vite dev server — `vite.config.js`

Add `server: { port: 5173, strictPort: true }` so the app fails loudly instead of moving to
5174 and breaking the Keycloak redirect URI match.

## Files

| File | Change |
|------|--------|
| [PlatinumCredentialManager.Client/src/config/keycloak.js](PlatinumCredentialManager.Client/src/config/keycloak.js) | Default export + env-overridable config |
| [PlatinumCredentialManager.Client/src/main.js](PlatinumCredentialManager.Client/src/main.js) | `keycloak.init({...})` then mount |
| `PlatinumCredentialManager.Client/public/silent-check-sso.html` | New — silent SSO helper |
| `PlatinumCredentialManager.Client/src/composables/useAuth.js` | New — reactive auth state + `login`/`logout` |
| [PlatinumCredentialManager.Client/src/App.vue](PlatinumCredentialManager.Client/src/App.vue) | Login/Logout UI driven by `useAuth()` |
| `PlatinumCredentialManager.Client/src/api/http.js` | New — Axios instance with bearer + refresh interceptors |
| [PlatinumCredentialManager.Client/vite.config.js](PlatinumCredentialManager.Client/vite.config.js) | Optional — pin `server.port` / `strictPort` |
| `PlatinumCredentialManager.Client/.env` | Optional — `VITE_KEYCLOAK_*` / `VITE_API_URL` |

## Verification

1. `docker compose up -d`; `http://localhost:8080/realms/platinum/.well-known/openid-configuration` returns JSON.
2. `cd PlatinumCredentialManager.Client && npm run dev` → open `http://localhost:5173`. App renders with a **Log in** button and is **not** redirected away.
3. Click **Log in** → Keycloak login page → sign in as the user from step 1 → returned to the app showing the username + **Log out**.
4. Devtools console: decode the token — `JSON.parse(atob(keycloak.token.split('.')[1]))` — and confirm `iss` = `http://localhost:8080/realms/platinum`.
5. Issue a request through `src/api/http.js` to `/category`; confirm the `Authorization: Bearer …` header is attached (Network tab). Full 200 requires the backend `Authority` / `Audience` / CORS fixes from blocking issue #5.
6. Click **Log out** → app returns to the logged-out state with the **Log in** button.

## Timeline & effort — Keycloak setup

**Hands-on effort: ~8–14 hours** for the plan as scoped (auth + Axios token wiring; no router guards, tests, or styling).

| Chunk | Effort | Where the time goes |
|-------|--------|---------------------|
| Run Keycloak + create user (step 1) | 0.5–1 h | First trip through the admin console |
| Frontend wiring (steps 2–6) | 3–5 h | Login redirect loop / "invalid redirect URI" / silent-SSO iframe cookie session |
| Axios interceptor + refresh (step 7) | 1–2 h | `updateToken` timing, 401 handling |
| Backend fixes + end-to-end (step 9 + verify) | 3–5 h | **Biggest risk:** `platinum-vue` tokens carry no `aud: platinum-api` claim, so a valid login still 401s until an audience mapper / client scope is added in Keycloak |

**At 30 min/day:** each session loses ~5–10 min to re-orientation, so effective throughput is ~20 min. That is **~25–35 sessions ≈ 5–7 weeks of weekdays (~1.5 months)**. If already comfortable with OIDC/Keycloak: ~3–4 h → **2–3 weeks**.

**Keeping momentum in 30-min chunks:**
- Follow the step order — each is independently testable.
- End each session on a green checkpoint (one verification item passing), never mid-debug.
- Steps 1–4 give a visible working login within ~4–5 sessions — front-load that milestone.
- Save steps 7 and 9 for their own 4–6 sessions; treat the audience-claim 401 as expected, not a surprise.

## Frontend design & build-out (future scope, beyond this plan)

The build is straightforward — a credential manager is CRUD. Making it *look good* is the variable, and a component library collapses that from "hard" to "assembly."

**What the frontend needs beyond auth:**
- **Routing** — `vue-router` (not installed): login, dashboard, categories, credentials list/detail, add/edit
- **State** — Pinia or plain composables for auth + cached lists
- **Views** — app shell/nav, category CRUD, credential list (grouped/filtered), credential detail with **reveal password + copy-to-clipboard**, add/edit forms with validation
- **The unglamorous ~40%** — loading / empty / error states, toasts, responsive layout, validation wiring

**Effort (on top of the auth work):**

| Piece | With a component library | Hand-rolled CSS |
|-------|--------------------------|-----------------|
| Router + layout + nav | 2–4 h | 3–5 h |
| Category CRUD | 3–4 h | 5–7 h |
| Credential CRUD + filter + reveal/copy | 6–9 h | 10–15 h |
| Styling / theming pass | 2–4 h | 6–12 h |
| Polish (states, responsive, toasts) | 3–5 h | 6–10 h |
| **Total** | **~16–26 h** | **~30–50 h** |

At ~20 min/session, the component-library path is **~50–80 sessions ≈ 2.5–4 months** of weekdays. Combined with the Keycloak work, this is a multi-month project at 30 min/day.

**How to make it easy:**
1. **Use a Vue component library** — the biggest single time-saver. [Naive UI](https://www.naiveui.com/) (lightweight, zero-config) or [PrimeVue](https://primevue.org/) (batteries-included, ships an admin template called "Sakai"). Their defaults are professionally designed, so no visual-design skill is needed — you make layout and hierarchy choices, not pixel choices.
2. **Scaffold from an admin template** rather than a blank `App.vue` — routing, layout, dark mode, and theming come pre-wired.
3. **One list pattern + one form-dialog pattern, reused** for Categories and Credentials.
4. **Don't hand-write validation** — use the library's field validation or `vee-validate`.
