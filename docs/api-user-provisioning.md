# API user provisioning — `/user/me` endpoint

> Reference for wiring a mapped endpoint that maps the authenticated JWT identity to a
> local `User` row. Extracted from the Stage III user-operations work.

## Context

Every protected endpoint group (`/creds`, `/category`) uses `.RequireAuthorization()`, so a
request already carries a validated JWT. What the API needs on top of that is a **local `User`
row** keyed by the token's `sub` claim ([Models/User.cs](PlatinumCredentialManager.Api/Models/User.cs)):

```csharp
public class User
{
    public int Id { get; set; }
    public required string KeycloakId { get; set; }  // the sub claim from the JWT
    public ICollection<Category> Categories { get; set; } = [];
}
```

`Category.UserId` is a required FK to that row, so a `Category` (and transitively a `Credential`)
can't be created until the caller's `User` exists.

**The gap:** nothing creates that row. `MapUserEndpoints` is an empty stub and isn't registered
in [Program.cs](PlatinumCredentialManager.Api/Program.cs). The seeder
([Data/DataExtensions.cs](PlatinumCredentialManager.Api/Data/DataExtensions.cs)) only creates a
single placeholder user for the demo data. So a real logged-in user gets `401` on every
`/category` and `/creds` call because the lookup `Users.FirstOrDefault(u => u.KeycloakId == sub)`
returns null.

This doc adds an explicit provisioning endpoint the frontend calls once after login, plus a
shared helper the other endpoints use as a fallback.

> **IdP-agnostic.** The pattern is "map JWT `sub` → local row." It holds whether the token comes
> from Keycloak or Supabase — both put the user id in `sub`. Only the `KeycloakId` field/column
> name is Keycloak-flavoured; rename to `SubjectId` / `ExternalId` if the provider changes.

## Design

| Route | Purpose | Response |
|-------|---------|----------|
| `POST /user/me` | Idempotent "make sure my row exists". Frontend calls this right after login. | `201` first time (with `Location: /user/me`), `200` after |
| `GET /user/me` | Read the current user's profile. | `200`, or `404` if never provisioned |

- **No request body.** Identity comes entirely from the bearer token — the client can't claim to
  be someone else.
- First provisioning also creates the user's own `"Miscallaneous"` category (the per-user default
  that credentials fall back to).
- `GetOrCreateUserAsync` is exposed as a `CredsStoreContext` extension so
  `CategoryEndpoints` / `CredentialEndpoints` can JIT-provision if `POST /user/me` was skipped.

## Code

### `PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs`

```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.User;
using PlatinumCredentialManager.Api.Models;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class UserEndpoints
{
    public const string MiscCategoryName = "Miscallaneous";
    private const string GetCurrentUserRoute = "GetCurrentUser";

    public static void MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/user").RequireAuthorization();

        // GET /user/me — profile for the logged-in user (404 if never provisioned)
        userGroup.MapGet("/me", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = KeycloakId(principal);
            if (keycloakId is null) return Results.Unauthorized();

            var dto = await dbContext.Users
                .Where(u => u.KeycloakId == keycloakId)
                .Select(u => new GetUserDto(
                    u.Id, u.KeycloakId,
                    u.Categories.Select(c => c.CategoryName).ToList()))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).WithName(GetCurrentUserRoute);

        // POST /user/me — "make sure my row exists". Frontend calls this right after login.
        // 201 the first time, 200 afterwards.
        userGroup.MapPost("/me", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = KeycloakId(principal);
            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users
                .Include(u => u.Categories)
                .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            bool created = user is null;
            if (user is null)
            {
                user = new User { KeycloakId = keycloakId };
                user.Categories.Add(new Category { CategoryName = MiscCategoryName });
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }

            var dto = new GetUserDto(
                user.Id, user.KeycloakId,
                user.Categories.Select(c => c.CategoryName).ToList());

            return created
                ? Results.CreatedAtRoute(GetCurrentUserRoute, null, dto)
                : Results.Ok(dto);
        });
    }

    // Reused by CategoryEndpoints / CredentialEndpoints as a JIT fallback.
    public static async Task<User?> GetOrCreateUserAsync(
        this CredsStoreContext db, ClaimsPrincipal principal)
    {
        var keycloakId = KeycloakId(principal);
        if (keycloakId is null) return null;

        var user = await db.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
        if (user is null)
        {
            user = new User { KeycloakId = keycloakId };
            user.Categories.Add(new Category { CategoryName = MiscCategoryName });
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        return user;
    }

    private static string? KeycloakId(ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value
        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
```

The `sub` fallback to `ClaimTypes.NameIdentifier` is because `JwtBearer` leaves `MapInboundClaims`
on by default, which renames `sub` to the long `nameidentifier` claim URI.

### `PlatinumCredentialManager.Api/Dtos/User/GetUserDto.cs` (new)

```csharp
namespace PlatinumCredentialManager.Api.Dtos.User;

public record GetUserDto(int Id, string KeycloakId, ICollection<string> Categories);
```

### `PlatinumCredentialManager.Api/Program.cs` — register the group

```csharp
app.MapCredentialEndpoints();
app.MapCategoryEndpoints();
app.MapUserEndpoints();      // add this
```

## Frontend usage

After a successful login (token stored), call `POST /user/me` with the bearer token **before**
the first `/category` or `/creds` request:

```js
await http.post('/user/me')   // idempotent — safe to call on every app load
```

Use the returned `GetUserDto` if the UI needs the local `Id` or the initial category list. Treat a
non-2xx here as "auth not ready" and hold back the rest of the data fetches.

## Notes / caveats

- **Keep `GetOrCreateUserAsync` as a fallback** in the other endpoint classes. Relying on the
  client to always call `POST /user/me` first is fragile; the helper means a missed call
  degrades to a silent JIT insert instead of a `401` wall.
- **`CreateUserDto` / `UpdateUserDto` are stale.** They carry `Email` + `Password`, which the IdP
  owns now. This endpoint takes no body — do not wire those DTOs to it. Delete or repurpose them.
- **Add a unique index on `KeycloakId`** before leaning on this further. Two concurrent
  first-requests can both see `user is null` and insert duplicate rows:
  ```csharp
  // CredsStoreContext.OnModelCreating
  modelBuilder.Entity<User>().HasIndex(u => u.KeycloakId).IsUnique();
  ```
  then `dotnet ef migrations add UserKeycloakIdUnique`. With the index in place, wrap the insert
  in a `try/catch (DbUpdateException)` that re-queries.
- **`POST` vs `PUT`.** `POST /user/me` reads as "run the provisioning action" and is the common
  choice. `PUT /user/me` (idempotent ensure) is also defensible. Do **not** make `GET` auto-create
  — GET must stay side-effect-free.
- **Per-user `"Miscallaneous"`.** `MiscCategoryName` is the single source of truth for the default
  category name; `CategoryEndpoints` should resolve *this user's* misc category by
  `UserId + CategoryName` rather than a hardcoded id.

## Files

| File | Change |
|------|--------|
| [PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs](PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs) | Implement `MapUserEndpoints` (`GET`/`POST /user/me`) + `GetOrCreateUserAsync` helper |
| `PlatinumCredentialManager.Api/Dtos/User/GetUserDto.cs` | New — response DTO |
| [PlatinumCredentialManager.Api/Program.cs](PlatinumCredentialManager.Api/Program.cs) | Call `app.MapUserEndpoints()` |
| [PlatinumCredentialManager.Api/Data/CredsStoreContext.cs](PlatinumCredentialManager.Api/Data/CredsStoreContext.cs) | Unique index on `User.KeycloakId` (+ migration) |
| `PlatinumCredentialManager.Api/Dtos/User/CreateUserDto.cs`, `UpdateUserDto.cs` | Stale (email/password) — remove or repurpose |

## Verification

1. `dotnet run --project PlatinumCredentialManager.Api`, then hit `/openapi/v1.json` (or Swagger
   UI) and confirm `GET /user/me` and `POST /user/me` are listed.
2. With a valid bearer token for a brand-new subject:
   - `POST /user/me` → `201`, body has a fresh `Id` and `Categories: ["Miscallaneous"]`.
   - `POST /user/me` again → `200`, same `Id`, no duplicate category.
   - `GET /user/me` → `200`, matches.
3. `GET /user/me` with a token whose subject was never provisioned → `404`.
4. Any `/user/*` call with no token → `401` (group-level `.RequireAuthorization()`).
5. In the DB, `Users` has exactly one row for that `KeycloakId` and `Categories` has one
   `"Miscallaneous"` row pointing at it.
