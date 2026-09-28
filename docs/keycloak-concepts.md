# Keycloak Concepts

A primer on how Keycloak is put together and how OIDC authentication actually
works underneath it. This is general — it's not a setup guide for this
specific project (see `keycloak-console-setup.md` and
`keycloak-frontend-setup.md` for that), and it's not a debugging log (see
`keycloak-auth-debugging.md` for that). This is the mental model.

## What problem Keycloak solves

Any app with user accounts needs to answer two questions on every request:
*who is this*, and *what are they allowed to do*. You could hand-roll
password storage, login forms, sessions, and token issuance yourself, but
that's a lot of security-critical code to get right (password hashing,
session fixation, CSRF, token replay, etc.).

Keycloak is an **Identity and Access Management (IAM)** server: it owns user
accounts, login screens, and token issuance, so your application never
touches a raw password. Your app's job shrinks to "receive a token from
Keycloak, decide whether to trust it." This pattern — a dedicated identity
provider issuing tokens that other apps trust — is called **OpenID Connect
(OIDC)**, which is itself an identity layer built on top of the **OAuth 2.0**
authorization framework.

## The building blocks

### Realm

A realm is an isolated namespace: its own users, clients, roles, and
settings. Nothing in one realm is visible to another. The built-in `master`
realm is for administering Keycloak itself — you never put your application's
users there. A real project creates its own realm (e.g. `platinum`) for that.

### Client

A client is *an application* that wants to authenticate users through the
realm — not a person. There are two kinds, and the distinction matters:

- **Public client** — can't keep a secret (a browser SPA, a mobile app;
  anyone can read its source). It authenticates using only its client ID and
  the redirect-based flow described below. This is what a frontend like a
  Vue or React app is.
- **Confidential client** — a server-side app that *can* keep a secret
  (a backend service). It authenticates with a client ID + client secret,
  and can be trusted with more powerful operations (like calling the Admin
  REST API on its own behalf, with no end user involved at all).

Every access token records which client it was issued to, via the `azp`
(authorized party) claim.

### User

A person's account: credentials, profile attributes (email, name), and
role/group memberships. Scoped to one realm.

### Role and Group

A **role** is a label representing a permission ("admin", "editor"). Roles
can be realm-wide or scoped to a specific client. A **group** is a bundle of
roles you can assign to many users at once. Neither is required to get basic
login working — they matter once you need authorization ("can this user
delete this resource"), not just authentication ("who is this user").

### Client scope

This is the one that trips people up. A client scope is a **named, reusable
bundle of protocol mappers** (see below) plus optional role mappings. Instead
of configuring the same set of claims on every single client, you define the
bundle once — say, a `profile` scope that adds `name`, `given_name`,
`family_name` — and attach it to as many clients as need it.

A scope is attached to a client one of two ways:

- **Default** — always applied, every token that client gets includes it.
- **Optional** — only applied if the token request explicitly asks for it
  (`&scope=the_scope_name` in the auth request).

Keycloak ships several **built-in** scopes out of the box when you create a
realm normally through the admin console: `profile`, `email`, `roles`,
`web-origins`, `acr`, `basic`, and others. These aren't magic — they're
ordinary client scopes with ordinary protocol mappers, just pre-created for
you. Critically: **they only exist if something actually created them.** A
realm built by importing a hand-edited or incomplete JSON export can end up
with clients that *reference* these scope names without the scopes actually
existing in the realm — Keycloak just logs a warning and silently skips the
reference. The client ends up with none of the claims those scopes would
normally have provided, and nothing in the UI makes that obvious until you
inspect a token.

### Protocol mapper

A protocol mapper is the actual mechanism that puts a specific claim into a
token — "take this user's database ID and put it in the `sub` claim," "take
this client's ID and add it to `aud`." A mapper lives in one of two places:

- **On a client scope** — shared by every client the scope is attached to.
- **Directly on a client** (a "dedicated" mapper) — applies to that one
  client only, regardless of what scopes it does or doesn't have.

Both produce the same *effect* on a token; the difference is just reuse.
Contrary to how it might feel from the admin console's layout, there's
nothing structurally special about the built-in scopes' mappers — you can
always add a claim to one client directly instead of relying on a scope,
which is the right move when a scope you'd depend on doesn't reliably exist.

### Identity provider (brief mention)

Keycloak can also delegate authentication to an external provider (Google,
GitHub, another company's Keycloak/SAML IdP) instead of storing the password
itself. Worth knowing this exists; not needed to understand the core flow.

## The flows Keycloak implements

These are OAuth2/OIDC "grant types" — different ways of getting a token,
suited to different situations.

### Authorization Code Flow (+ PKCE) — the redirect flow

The standard flow for anything with a browser, and the only one recommended
for public clients today:

1. The app redirects the browser to Keycloak's login page, including its
   client ID and a `redirect_uri`.
2. The user authenticates directly with Keycloak — the app's code never sees
   the password.
3. Keycloak redirects back to `redirect_uri` with a short-lived, one-time
   **authorization code** in the URL.
4. The app exchanges that code for tokens at Keycloak's token endpoint.

**PKCE** (Proof Key for Code Exchange) is a required hardening on top of this
for public clients: before step 1, the app generates a random secret and
sends only its hash; at step 4, it must present the original secret. This
stops a malicious app on the same device from intercepting the authorization
code and completing the exchange itself.

### Direct Access Grant (Resource Owner Password Credentials)

The app collects a username and password itself and posts them straight to
Keycloak's token endpoint — no redirect, no login page. It's simpler to
integrate but means the app *does* see the raw password, and it can't
support things like social login, MFA screens, or "remember this device."
It's disabled by default on new clients and generally only left on for
trusted first-party use (or testing) — never for third-party apps.

### Client Credentials

A confidential client authenticates as *itself*, with no end user involved
at all — client ID + client secret in, access token out. This is how a
backend service calls another service's API on its own authority (e.g. a
server calling the Keycloak Admin REST API to provision an account).

### Refresh Token

Access tokens are deliberately short-lived (minutes). Instead of forcing a
full re-login when one expires, the client holds onto a longer-lived
**refresh token** and exchanges it for a new access token silently.

## Tokens

A login gets you up to three distinct tokens, easy to conflate:

| Token | Audience | Purpose |
|---|---|---|
| **ID token** | The client app | "Here's who logged in" — for the frontend to know the user's identity |
| **Access token** | The API(s) being called | "Here's proof of who's calling, present this to a resource server" |
| **Refresh token** | Keycloak itself | "Let me get a new access token without asking the user to log in again" |

The access token is the one your API validates — the ID token is for the
frontend to introspect, not for calling other services. Mixing these up
(e.g. sending an ID token to an API expecting an access token) is a common
source of confusing auth bugs.

### JWT anatomy

All three are usually **JWTs** (JSON Web Tokens): three base64url-encoded
segments joined by dots, `header.payload.signature`. The middle segment is
plain JSON once decoded — no secret needed to *read* it, only to verify its
signature:

```python
import base64, json
payload = token.split('.')[1]
payload += '=' * (-len(payload) % 4)   # restore base64 padding
print(json.loads(base64.urlsafe_b64decode(payload)))
```

Common claims you'll see in the payload:

| Claim | Meaning |
|---|---|
| `iss` | Issuer — which realm minted this token |
| `sub` | Subject — the unique ID of the authenticated user |
| `aud` | Audience — which resource server(s) this token is valid for |
| `azp` | Authorized party — which client requested it |
| `exp` / `iat` | Expiry / issued-at, as Unix timestamps |
| `jti` | Unique token ID (useful for revocation lists) |
| `scope` | Which scopes were actually applied to this token |
| `typ` | Token type, usually `"Bearer"` |

A JWT missing a claim you expected isn't a validation failure by itself — it
just means nothing added that claim when the token was built. Decoding the
token directly, rather than assuming what's in it, is the fastest way to
tell "the claim isn't there" apart from "the claim is there but something
else is rejecting it."

### Lightweight access tokens

Keycloak also supports issuing a stripped-down access token containing only
the bare mandatory claims, omitting anything a mapper would normally add,
unless that mapper is explicitly flagged to survive it. This trades a
smaller token for fewer claims — worth knowing this mode exists, since a
token missing claims you configured can mean either "the mapper isn't
attached" or "the mapper is attached but not flagged for lightweight mode."

## How a resource server validates a token

Your API doesn't need its own copy of Keycloak's private key or a network
call on every single request. Instead:

1. **Discovery** — it fetches `{realm-url}/.well-known/openid-configuration`
   once, which tells it the token endpoint, the issuer's canonical URL, and
   where to find signing keys (`jwks_uri`).
2. **Signature verification** — it fetches Keycloak's public keys (JWKS) and
   checks the token's signature against them, proving Keycloak actually
   issued it and it hasn't been tampered with.
3. **Claim validation** — separately from the signature, it checks the
   claims make sense for *this* API: `exp` hasn't passed, `iss` matches the
   expected realm, and — critically — `aud` includes this API's own
   identifier.

That last check, audience validation, is a security boundary: without it, a
token minted for one API could be replayed against a completely different
API in the same organization, as long as the two happen to trust the same
identity provider. An API should reject a token that wasn't meant for it,
even if that token is perfectly validly signed by a realm it trusts.

## Two ways to configure Keycloak (same underlying state)

- **Admin Console** — the web UI at `/admin`. Good for exploring and one-off
  changes.
- **Admin REST API** — `/admin/realms/{realm}/...` endpoints, requiring an
  admin (or sufficiently-privileged service account) token. Everything the
  console does, it does by calling this API under the hood. Useful for
  scripting, automation, or making a change without waiting on a page reload.

Both act on the **live, running realm** immediately.

### Realm import/export (JSON)

A realm's entire configuration — clients, scopes, mappers, roles, and
optionally users — can be represented as one JSON document. This is used to
version-control a realm's config or spin up a reproducible environment (e.g.
via `docker run ... --import-realm`).

The important gotcha: import typically only happens at **startup**, and
commonly *only if the realm doesn't already exist yet* in the server's
storage. Editing the JSON file after the realm has already been created does
nothing on its own — the running realm and the file on disk are two separate
things that only sync at the moment of a fresh import. Changes made live
(via console or API) don't get written back into the file automatically
either. Keeping the two in agreement is a manual discipline, not something
Keycloak does for you.

## Mental model, condensed

- A **realm** isolates everything; a **client** is an app, not a person.
- **Scopes** are just named bundles of **mappers**; mappers are what actually
  put a claim in a token. Nothing stops you from adding a mapper directly to
  a client instead of via a scope.
- Built-in scopes (`profile`, `email`, `roles`, ...) are ordinary scopes that
  happen to be pre-created — they are not guaranteed to exist if a realm was
  built from a hand-edited or partial export.
- **ID token** is for the app; **access token** is for the API; don't
  confuse them.
- A missing claim in a token means some mapper isn't wired up — it doesn't
  by itself mean anything is "broken" at the protocol level.
- **Audience validation** exists specifically so a token for one API can't
  be replayed against another.
- The JSON export and the live running realm are two different sources of
  truth that only agree right after an import — assume they've drifted
  unless you've just synced them.
