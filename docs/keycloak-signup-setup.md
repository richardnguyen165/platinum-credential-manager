# Custom sign-up with Keycloak (SignUp.vue → API → Admin REST API)

> **Deferred (2026-09-10)** — shipping Keycloak's **hosted** registration page first
> (`registrationAllowed: true` + `keycloak.register()`; see
> [keycloak-frontend-setup.md](keycloak-frontend-setup.md)). This custom-form + Admin-API
> approach is the fallback if the hosted sign-up UX becomes a problem. Not started.
>
> Step-by-step plan + official documentation links for building the registration flow.
> Companion to [keycloak-frontend-setup.md](keycloak-frontend-setup.md) (login/logout) and
> [api-user-provisioning.md](api-user-provisioning.md) (local `User` row after login).

## Context

Keycloak has **no register endpoint for a public client**. `grant_type=password` at the token
endpoint authenticates *existing* users only; there is no direct-grant equivalent for creating
one. The two ways to register from a custom in-app form:

- **(a)** `registrationAllowed: true` + redirect to Keycloak's hosted registration page — breaks
  "the user never leaves the app."
- **(b)** `SignUp.vue` → `POST /user/register` on our ASP.NET Core API → Keycloak **Admin REST
  API** (`POST /admin/realms/platinum/users`). The API holds a privileged service-account
  credential that must never reach the browser.

**This doc implements (b).** The local `User` row is still created afterwards by the existing
`POST /user/me` on first login — registration only creates the *Keycloak* account.

## Reference documentation

### Step 1 — service-account client (numbered procedures in the guide)

| Topic | Link |
|-------|------|
| Create an OIDC client | https://www.keycloak.org/docs/latest/server_admin/index.html#proc-creating-oidc-client_server_administration_guide |
| Turn on the service account (+ shows the `client_credentials` token request) | https://www.keycloak.org/docs/latest/server_admin/index.html#_service_accounts |
| Assign the `manage-users` role to it | https://www.keycloak.org/docs/latest/server_admin/index.html#proc-assigning-role-mappings_server_administration_guide |

### Step 2 — API calls the Admin REST API

| Topic | Link |
|-------|------|
| Admin REST API reference — **Users** section: `POST /admin/realms/{realm}/users`, `PUT .../users/{id}/reset-password` | https://www.keycloak.org/docs-api/latest/rest-api/index.html |
| Same, pinned to our version (26.2.5) | https://www.keycloak.org/docs-api/26.2.5/rest-api/index.html |
| OIDC token endpoint & grant types (the `client_credentials` call for the admin token) | https://www.keycloak.org/docs/latest/securing_apps/index.html |
| Official Java admin-client library (reference only — no .NET equivalent, we hand-roll `HttpClient`) | https://www.keycloak.org/securing-apps/admin-client |

### Step 3 — frontend

| Topic | Link |
|-------|------|
| keycloak-js adapter (`init` / `login` / `updateToken`) — note `useAuth.js` bypasses this for login | https://www.keycloak.org/securing-apps/javascript-adapter |

### Version note

We run **26.2.5**; `latest` currently serves 26.7.x — screens are nearly identical. If something
differs, use the archive: https://www.keycloak.org/documentation-archive → 26.2.5, or swap
`latest` → `26.2.5` in the `/docs/` URLs.

### Narrative tutorials (unofficial, prose walkthroughs)

- Baeldung — Keycloak Admin REST API: https://www.baeldung.com/java-keycloak-admin-rest-api
- Baeldung — user registration: https://www.baeldung.com/keycloak-user-registration

  (Java examples, but the HTTP calls are language-agnostic.)

## Prerequisite: fix the broken token URL in `useAuth.js`

[useAuth.js](PlatinumCredentialManager.Client/src/composables/useAuth.js) line 13 does
`const { url, realm, clientId } = keycloak` — `keycloak.url` does not exist (it is
`authServerUrl`), and `realm`/`clientId` are unset until `.init()` runs, so `TOKEN_URL` becomes
`undefined/realms/undefined/...`. `signup()` calls `login()` internally, so this must be fixed
first. Replace with plain constants (per [keycloak-frontend-setup.md](keycloak-frontend-setup.md)
changes-required #3):

```js
const KEYCLOAK_URL = import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080'
const REALM       = import.meta.env.VITE_KEYCLOAK_REALM ?? 'platinum'
const CLIENT_ID   = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'platinum-vue'
const API_URL     = import.meta.env.VITE_API_URL ?? 'http://localhost:5142'
const TOKEN_URL  = `${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token`
const LOGOUT_URL = `${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/logout`
```

Then swap `clientId` → `CLIENT_ID` in `login` / `refresh` / `logout`.

## Steps

### 1. Keycloak — create the admin service-account client

Admin console (`http://localhost:8080`, `admin`/`admin`), realm **platinum**:

1. **Clients → Create client**
   - Client ID: `platinum-api-admin`
   - **Client authentication: On** (confidential)
   - Uncheck *Standard flow* and *Direct access grants*; check **Service accounts roles** → Save
2. **Credentials** tab → copy the **Client secret**
3. **Service account roles** tab → **Assign role** → filter *by clients* → `realm-management` →
   assign **manage-users** (add **view-users** too if the API should check existence before
   creating)

The `keycloak_data` volume in [docker-compose.yml](docker-compose.yml) persists this across
`docker compose up/down` (not `down -v`). For reproducibility, also add a `platinum-api-admin`
client stub to [keycloak/platinum-realm.json](keycloak/platinum-realm.json); the role mapping is
easiest left in the console.

### 2. API — `POST /user/register`

**`PlatinumCredentialManager.Api/.env`** — add:

```
KEYCLOAK_URL=http://localhost:8080
KEYCLOAK_REALM=platinum
KEYCLOAK_ADMIN_CLIENT_ID=platinum-api-admin
KEYCLOAK_ADMIN_CLIENT_SECRET=<secret from step 1>
```

**`PlatinumCredentialManager.Api/Keycloak/KeycloakAdminClient.cs`** (new):

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PlatinumCredentialManager.Api.Keycloak;

public sealed class KeycloakAdminClient(HttpClient http, IConfiguration cfg)
{
    private readonly string _url    = cfg["KEYCLOAK_URL"] ?? "http://localhost:8080";
    private readonly string _realm  = cfg["KEYCLOAK_REALM"] ?? "platinum";
    private readonly string _id     = cfg["KEYCLOAK_ADMIN_CLIENT_ID"]!;
    private readonly string _secret = cfg["KEYCLOAK_ADMIN_CLIENT_SECRET"]!;

    /// <returns>true = created, false = already exists (409)</returns>
    public async Task<bool> CreateUserAsync(string email, string password, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(ct);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_url}/admin/realms/{_realm}/users");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new
        {
            email,
            username = email,              // realm has registrationEmailAsUsername = true
            enabled = true,
            emailVerified = true,
            credentials = new[] { new { type = "password", value = password, temporary = false } }
        });

        var res = await http.SendAsync(req, ct);
        if (res.StatusCode == HttpStatusCode.Conflict) return false;
        res.EnsureSuccessStatusCode();
        return true;
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        var res = await http.PostAsync($"{_url}/realms/{_realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = _id,
                ["client_secret"] = _secret,
            }), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<TokenResponse>(ct))!.access_token;
    }

    private sealed record TokenResponse(string access_token);
}
```

**[Program.cs](PlatinumCredentialManager.Api/Program.cs)** — before `builder.Build()`:

```csharp
builder.Services.AddHttpClient<KeycloakAdminClient>();
```

**[UserEndpoints.cs](PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs)** — add **outside**
`userGroup` (that group has `.RequireAuthorization()`; register must be anonymous):

```csharp
app.MapPost("/user/register", async (RegisterDto dto, KeycloakAdminClient keycloak) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest(new { error = "Email and password are required" });

    var created = await keycloak.CreateUserAsync(dto.Email, dto.Password);
    return created
        ? Results.Created("/user/me", null)
        : Results.Conflict(new { error = "An account with that email already exists" });
});

public record RegisterDto(string Email, string Password);
```

### 3. Frontend — `signup()` + wire the form

**[useAuth.js](PlatinumCredentialManager.Client/src/composables/useAuth.js)** — add (after the
prerequisite fix above):

```js
async function signup(email, password) {
    try {
        await axios.post(`${API_URL}/user/register`, { email, password })
        return await login(email, password)   // account exists now — log straight in
    }
    catch (err) {
        return {
            status: err.response?.status,
            error: err.response?.data?.error ?? 'Sign up failed'
        }
    }
}

export default function useAuth() {
  return { authState, login, logout, refresh, signup }
}
```

**[SignUp.vue](PlatinumCredentialManager.Client/src/views/SignUp.vue)** — replace (current
`import { authState, login }` is broken — `useAuth.js` has no named exports):

```vue
<template>
  <form @submit.prevent="handleSubmit">
    <input v-model="email" type="email" required />
    <input v-model="password" type="password" required />
    <button type="submit" :disabled="loading">Sign Up</button>
    <p v-if="error">{{ error }}</p>
  </form>
</template>

<script setup>
import { ref } from "vue"
import { useRouter } from "vue-router"
import useAuth from "@/composables/useAuth"

const { signup } = useAuth()
const router = useRouter()
const email = ref(""), password = ref(""), error = ref(""), loading = ref(false)

async function handleSubmit() {
    loading.value = true; error.value = ""
    const { error: err } = await signup(email.value, password.value)
    loading.value = false
    if (err) { error.value = err; return }
    router.push("/")
}
</script>
```

## Files

| File | Change |
|------|--------|
| Keycloak realm (console or [platinum-realm.json](keycloak/platinum-realm.json)) | New confidential client `platinum-api-admin` + `manage-users` on its service account |
| [PlatinumCredentialManager.Api/.env](PlatinumCredentialManager.Api/.env) | `KEYCLOAK_URL` / `KEYCLOAK_REALM` / `KEYCLOAK_ADMIN_CLIENT_ID` / `KEYCLOAK_ADMIN_CLIENT_SECRET` |
| `PlatinumCredentialManager.Api/Keycloak/KeycloakAdminClient.cs` | New — client-credentials token + create-user call |
| [PlatinumCredentialManager.Api/Program.cs](PlatinumCredentialManager.Api/Program.cs) | `AddHttpClient<KeycloakAdminClient>()` |
| [PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs](PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs) | `POST /user/register` (anonymous) + `RegisterDto` |
| [PlatinumCredentialManager.Client/src/composables/useAuth.js](PlatinumCredentialManager.Client/src/composables/useAuth.js) | Prereq URL-constant fix + `signup()` + export it |
| [PlatinumCredentialManager.Client/src/views/SignUp.vue](PlatinumCredentialManager.Client/src/views/SignUp.vue) | Real form wired to `signup()` |

## Verification

1. `docker compose up -d`; `dotnet run --project PlatinumCredentialManager.Api`; `npm run dev`.
2. `curl` the service-account token directly to confirm step 1:
   `curl -d grant_type=client_credentials -d client_id=platinum-api-admin -d client_secret=… http://localhost:8080/realms/platinum/protocol/openid-connect/token`
   → JSON with `access_token`.
3. `/signup` → submit → `201` from `/user/register`, then `200` from the Keycloak token endpoint,
   then redirect to `/`.
4. Admin console → realm **platinum** → **Users** shows the new account.
5. Submit the same email again → `409` → "already exists" shown in the form.
6. Log out, then log in with the new credentials on `/login` → succeeds; `POST /user/me` then
   creates the local row with a `"Miscallaneous"` category.
