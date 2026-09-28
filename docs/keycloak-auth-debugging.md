# Debugging the "login redirects back to Home" bug

This documents how a `POST /user/me` → `401 Unauthorized` bug was tracked down and
fixed. The symptom was simple ("login works, but you always end up back on the
Home page"), but the actual cause was two separate, stacked Keycloak
misconfigurations. Written up because the debugging *method* here — proving
where in the pipeline a request fails before guessing why — is as useful to
study as the fix itself.

## The symptom

1. Click **Log In** on the homepage → redirected to Keycloak → authenticate →
   redirected back to the app.
2. The app never leaves `/`. It should redirect to `/cred-homepage/:user_id`
   once the backend confirms who you are.

## Why "redirect" was the wrong thing to debug

The frontend redirect to `/cred-homepage/:user_id` only happens *after*
[`useAuth.js`](../PlatinumCredentialManager.Client/src/composables/useAuth.js)'s
`onAuthSuccess` callback successfully calls `POST /user/me` and gets back a
user `id`:

```js
keycloak.onAuthSuccess = async () => {
  authState.authenticated = true
  try {
    const { data } = await axios.post(`${BACKEND_URL}/user/me`, null, {
      headers: { Authorization: `Bearer ${keycloak.token}` }
    })
    authState.userId = data.id   // <-- HomePage.vue watches this and redirects
  } catch (err) {
    console.error('Failed to provision user', err)
  }
}
```

So "it doesn't redirect" wasn't a routing bug — it meant the `try` block was
throwing, `authState.userId` never got set, and the redirect-on-watch in
`HomePage.vue` simply had nothing to react to. The real question was: **why is
`POST /user/me` failing?**

## Step 1: confirm it's actually a 401, and where the 401 comes from

Opening DevTools → Network showed `POST /user/me` → `401`. But a 401 can come
from two completely different places in this codebase:

- **The authentication middleware itself**, if the JWT fails validation
  (bad signature, wrong issuer, wrong audience, expired). ASP.NET Core's
  `JwtBearerHandler` attaches a `WWW-Authenticate` header describing *why*.
- **The endpoint's own code**, in
  [`UserEndpoints.cs`](../PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs):

  ```csharp
  var keycloakId = KeycloakId(principal);
  if (keycloakId is null) return Results.Unauthorized();
  ```

  This is a bare `Results.Unauthorized()` — no `WWW-Authenticate` header at
  all. It fires when the token is otherwise *valid* but doesn't contain a
  claim the code can use to identify the user.

These two failure modes look identical in the browser (both are just "401"),
but they have completely different causes. **The fix to try depends entirely
on which one it is.** Guessing here wastes time — the header tells you for
free:

```bash
# No token: generic challenge, no error detail
curl -i -X POST localhost:5142/user/me
# WWW-Authenticate: Bearer

# Garbage token: middleware explicitly rejects it
curl -i -X POST localhost:5142/user/me -H "Authorization: Bearer garbage.garbage.garbage"
# WWW-Authenticate: Bearer error="invalid_token"

# The browser's real token
curl -i -X POST localhost:5142/user/me -H "Authorization: Bearer $REAL_TOKEN"
# (no WWW-Authenticate header at all)
```

No header on the real token = **authentication succeeded**. The request
reached `UserEndpoints.cs`'s own code, and *that* returned the 401. This
ruled out "bad JWT" and pointed straight at `KeycloakId(principal)`:

```csharp
private static string? KeycloakId(ClaimsPrincipal principal) =>
    principal.FindFirst("sub")?.Value
    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

Both lookups were returning null. Which meant: **the access token had no
usable subject claim at all.**

## Step 2: look at the actual token, not what you assume is in it

Never assume a JWT's shape — decode it. The middle segment of a JWT
(`header.payload.signature`) is just base64url-encoded JSON:

```bash
python3 -c "
import base64, json
payload = token.split('.')[1]
payload += '=' * (-len(payload) % 4)   # base64url needs padding restored
print(json.dumps(json.loads(base64.urlsafe_b64decode(payload)), indent=2))
"
```

The decoded access token was:

```json
{
  "exp": 1790570235,
  "iat": 1790569935,
  "jti": "...",
  "iss": "http://localhost:8080/realms/platinum",
  "aud": "platinum-api",
  "typ": "Bearer",
  "azp": "platinum-vue",
  "sid": "...",
  "scope": ""
}
```

No `sub`. No `preferred_username`. No `email`. And notably, `scope` is an
**empty string** — for a client with default scopes configured, that's a red
flag, not normal.

## Step 3: find out why the token is missing claims

This came in two layers, discovered in order.

### Layer 1 (fixed first, and necessary but not sufficient): missing audience

The API's JWT validation requires the token's `aud` to contain `platinum-api`:

```csharp
options.Audience = "platinum-api";
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateAudience = true,
    ...
};
```

But `platinum-vue` (the client the frontend authenticates as) had **no
protocol mappers at all** — nothing was adding `platinum-api` to its tokens'
`aud`. Every token it issued failed audience validation outright (this
produced the `WWW-Authenticate: error="invalid_token"` version of the 401,
before layer 2 was even reachable).

Fix: add an **audience mapper** directly on the `platinum-vue` client, so its
tokens carry `aud: platinum-api`:

```json
{
  "name": "platinum-api-audience",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-audience-mapper",
  "consentRequired": false,
  "config": {
    "included.client.audience": "platinum-api",
    "id.token.claim": "false",
    "access.token.claim": "true"
  }
}
```

A subtlety that cost real time here: **this mapper has to live on the client
that *issues* the token** (`platinum-vue`), not on the client the token is
*for* (`platinum-api`). Both clients have near-identical JSON boilerplate in
the realm export, which made it easy to paste the mapper into the wrong
client object — the fix silently did nothing until it was moved to the right
one. Confirming a mapper actually applied means querying the client it's
*attached to*, not just checking the JSON file:

```bash
curl -s "http://localhost:8080/admin/realms/platinum/clients/$CLIENT_UUID/protocol-mappers/models" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

### Layer 2 (the actual root cause): the realm's built-in client scopes don't exist

Fixing the audience got `aud` into the token, but `sub` was *still* missing.
Normally `sub` (and `preferred_username`, `email`, etc.) are delivered by
protocol mappers that live on Keycloak's **built-in client scopes** —
`basic`, `profile`, `email`, `roles`, and so on. `platinum-vue`'s config
*references* these by name:

```json
"defaultClientScopes": ["web-origins", "acr", "profile", "roles", "basic", "email"]
```

But referencing a scope by name only works if that scope actually exists in
the realm. Every single Keycloak startup log for this project had been
quietly saying it didn't:

```
WARN [RepresentationToModel] Referenced client scope 'basic' doesn't exist. Ignoring
WARN [RepresentationToModel] Referenced client scope 'profile' doesn't exist. Ignoring
WARN [RepresentationToModel] Referenced client scope 'email' doesn't exist. Ignoring
... (etc, for every default/optional scope)
```

Confirmed directly against the live realm:

```bash
curl -s "http://localhost:8080/admin/realms/platinum/client-scopes" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
# -> only "offline_access" exists. basic/profile/email/roles/web-origins/acr: none of them.
```

`platinum-realm.json`'s top-level `clientScopes` array — the section that
would actually *define* `basic`/`profile`/etc. and their mappers — was
incomplete in this realm export. So `platinum-vue` was, in practice,
attached to zero scopes, no matter what its `defaultClientScopes` list said.
That's also why `scope: ""` showed up empty in step 2 — there was nothing to
list.

Properly fixing this means reconstructing Keycloak's standard scope
definitions (each with the several mappers they normally carry) inside the
realm export — a lot of boilerplate to get exactly right by hand. The
pragmatic fix, consistent with how the audience mapper was already solved,
was to stop depending on the (broken) scope system for the one claim that
actually matters here, and add `sub` as its own **dedicated mapper directly
on the client**:

```json
{
  "name": "sub",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-usermodel-property-mapper",
  "consentRequired": false,
  "config": {
    "user.attribute": "id",
    "claim.name": "sub",
    "jsonType.label": "String",
    "id.token.claim": "true",
    "access.token.claim": "true",
    "userinfo.token.claim": "true"
  }
}
```

This maps the Keycloak user's internal `id` straight onto the `sub` claim,
independent of whichever scopes are or aren't attached.

## Step 4: verify the fix in isolation before trusting it

Rather than testing through the browser (slow feedback loop, and conflates
frontend state with backend state), the fix was verified directly:

1. Create a throwaway Keycloak user via the admin API.
2. Get a real token for it via the password grant (`platinum-vue` has
   `directAccessGrantsEnabled: true`, so this works without a browser).
3. Decode the token and confirm `sub` and `aud` are both present.
4. `curl -X POST /user/me` with that token directly against the real API.

```bash
curl -s -X POST "http://localhost:8080/realms/platinum/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=platinum-vue&grant_type=password&username=$U&password=$P"
```

Result: `201 Created`, `{"id":2,"keycloakId":"...","categories":["Miscallaneous"]}`.
That's the exact response `UserEndpoints.cs`'s `POST /user/me` returns on
successful first-time provisioning — confirming the whole chain (JWT →
audience check → subject claim → DB row creation) end to end, without
needing to also trust the frontend's redirect logic at the same time.

The throwaway user and test data were deleted afterward; nothing about this
verification step is left behind.

## Applying config changes to Keycloak: two ways, one trap

Keycloak only reads `platinum-realm.json` at **container startup** via
`--import-realm`, and — critically — it **skips the import entirely if the
realm already exists** in its data volume. That means editing the JSON file
alone does nothing to an already-running realm; the container has to be
recreated with its volume wiped (`docker compose down -v && docker compose
up -d`) before a file edit takes effect. That wipe also deletes every user
and every other change made through the admin console in the meantime — an
easy way to lose test accounts and undo other live tweaks if you're not
careful about ordering.

The alternative used repeatedly here — hitting the **Admin REST API**
directly (`POST /admin/realms/platinum/clients/{id}/protocol-mappers/models`)
— changes the *live* realm immediately, with no restart and no data loss.
That's the faster loop for iterating on a fix. The JSON file should still be
updated afterward so the fix survives the next time someone *does* need to
wipe the volume (e.g. to pick up a change that isn't achievable live, like
adding a brand-new client).

## Summary of the actual fix

Two dedicated protocol mappers, both added directly to the `platinum-vue`
client (live via the admin API, and mirrored into
[`keycloak/platinum-realm.json`](../keycloak/platinum-realm.json) for
persistence):

| Mapper | Type | Purpose |
|---|---|---|
| `platinum-api-audience` | `oidc-audience-mapper` | Puts `aud: platinum-api` in the token so the API's audience validation passes |
| `sub` | `oidc-usermodel-property-mapper` | Puts the user's Keycloak ID into the `sub` claim, so `UserEndpoints.cs` can identify who's calling |

Both were needed. Fixing only the audience got past JWT validation but still
401'd (from the endpoint's own `Results.Unauthorized()` this time, not the
middleware) because `sub` was still missing.

## Still outstanding

The underlying defect — the realm export missing its built-in client scopes
— hasn't been fixed, only worked around for the one claim (`sub`) that was
blocking login. `preferred_username` and `email` are still absent from
tokens (they'd normally come from the `profile`/`email` scopes), which is
why `authState.username` in `useAuth.js` will read as an empty string. If
that's needed later, either add more dedicated mappers the same way, or
properly rebuild the realm's `clientScopes` definitions.
