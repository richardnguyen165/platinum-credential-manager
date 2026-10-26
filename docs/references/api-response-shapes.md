# API Response Shapes

What every C# endpoint in `PlatinumCredentialManager.Api` sends back, for each status code it can return.

Sources: [Endpoints/](../../PlatinumCredentialManager.Api/Endpoints/), [Dtos/](../../PlatinumCredentialManager.Api/Dtos/), [Program.cs](../../PlatinumCredentialManager.Api/Program.cs).

---

## Conventions that apply everywhere

- **JSON casing:** ASP.NET's default web JSON settings are in effect, because `Program.cs` never calls `ConfigureHttpJsonOptions`. So C# `CategoryName` is sent as `categoryName`.
- **Dates:** `DateOnly` is serialized as a string: `"2026-10-26"`.
- **`username`:** the DTOs declare it as `string?`, but the `Credential` model defaults it to `""`, and every write path stores `?? ""`. In practice it comes back as `""` and never as `null`. Type it `string` on the frontend, and treat `""` as "no username".
- **Auth:** every group uses `.RequireAuthorization()`. There are two kinds of 401:
  1. **The JWT is missing or invalid.** The JwtBearer middleware rejects the request before the handler runs. The **body is empty**, and the response carries a `WWW-Authenticate: Bearer ...` header.
  2. **The token is valid but there is no `sub` claim, or no `User` row for it.** The handler returns a ProblemDetails body (see below) with `detail: "User authentication expired."`. Every endpoint except `GET /user/me` and `POST /user/me` does the user-row lookup. That means **`POST /user/me` must be called before anything else**, or every call will 401.

### Error body formats

The endpoints use three different error styles, so the frontend needs to handle all of them:

| Style | Produced by | Content-Type | Body |
| --- | --- | --- | --- |
| **ProblemDetails** | `Results.Problem(...)` | `application/problem+json` | `ProblemDetails` object (below) |
| **Bare string** | `Results.BadRequest("...")` | `application/json` | a JSON string, e.g. `"Category cannot be blank!"` |
| **Empty** | `Results.NotFound()`, JwtBearer 401 | none | no body |

**ProblemDetails**, the output of `Results.Problem(...)`:

```
{
  type: string,     // RFC link filled in from the status, e.g. "https://tools.ietf.org/html/rfc9110#section-15.5.1"
  title: string,    // the code always sets this, e.g. "400 Bad Request"
  status: number,
  detail: string,   // human-readable message; show this to the user
  traceId: string   // may or may not be present, depending on .NET runtime defaults
}
```

### DTO validation failures (400)

`builder.Services.AddValidation()` enforces the `[Required]` / `[StringLength(100)]` attributes on request DTOs, and it does this **before** the handler runs. Failures return a validation problem:

**ValidationProblemDetails**:

```
{
  type: string,
  title: string,     // "One or more validation errors occurred."
  status: number,    // always 400
  errors: {
    <fieldName>: string[]   // e.g. CategoryName: ["The field CategoryName must be a string with a maximum length of 100."]
  }
}
```

If the JSON body is malformed or missing, the request also fails with a 400 before the handler runs.

---

## Shared response types

These are the C# DTOs as they arrive over the wire. The names in bold are the labels the endpoint tables below use.

**CategorySummary** ([CategorySummaryDto.cs](../../PlatinumCredentialManager.Api/Dtos/Category/CategorySummaryDto.cs))

```
{
  id: number,
  categoryName: string
}
```

**CategoryListItem** ([GetAllCategoryDto.cs](../../PlatinumCredentialManager.Api/Dtos/Category/GetAllCategoryDto.cs))

```
{
  categoryName: string,
  id: number
}
```

**CredentialSummary** ([GetAllCredentialsDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/GetAllCredentialsDto.cs)). No password.

```
{
  id: number,
  serviceName: string,
  username: string,          // "" when none
  dateCreated: string,       // "YYYY-MM-DD"
  dateLastUpdated: string    // "YYYY-MM-DD"
}
```

**CategoryDetail** ([GetDetailedCategoryDto.cs](../../PlatinumCredentialManager.Api/Dtos/Category/GetDetailedCategoryDto.cs))

```
{
  id: number,
  categoryName: string,
  credentials: CredentialSummary[]
}
```

**ExportCredential** ([ExportAllCredentialsDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/ExportAllCredentialsDto.cs)). Includes the password.

```
{
  id: number,
  serviceName: string,
  username: string,
  password: string,
  dateCreated: string,
  dateLastUpdated: string
}
```

**ExportCategory** ([ExportAllCategoriesDto.cs](../../PlatinumCredentialManager.Api/Dtos/Category/ExportAllCategoriesDto.cs))

```
{
  id: number,
  categoryName: string,
  credentials: ExportCredential[]
}
```

**CredentialDetail** ([GetDetailedCredentialDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/GetDetailedCredentialDto.cs)). Includes the password.

```
{
  id: number,
  serviceName: string,
  username: string,
  password: string,
  dateCreated: string,
  dateLastUpdated: string,
  categoryName: string,
  categoryId: number
}
```

**CredentialSearchResult** ([SearchCredentialResultsDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/SearchCredentialResultsDto.cs)). The id field is `credentialId`, not `id`.

```
{
  credentialId: number,
  categoryId: number,
  categoryName: string,
  serviceName: string,
  dateCreated: string,
  dateLastUpdated: string
}
```

**UserProfile** ([GetUserDto.cs](../../PlatinumCredentialManager.Api/Dtos/User/GetUserDto.cs))

```
{
  id: number,
  keycloakId: string,
  categories: CategorySummary[]
}
```

**ImportProblem**: the error body of the import endpoints. It is a ProblemDetails object with every error counter added at the top level.

```
{
  type: string,
  title: string,
  status: number,
  detail: string,
  <errorCounterName>: number   // one entry per counter; see the import endpoints below
}
```

---

## Misc

### `GET /antiforgery/token`

Source: [Program.cs](../../PlatinumCredentialManager.Api/Program.cs)

| Status | Body |
| --- | --- |
| 200 | **empty** |
| 401 | empty (JwtBearer) |

This endpoint has a side effect: it sets two cookies, the antiforgery cookie `PCMAntiForgeryCookie` (HttpOnly) and `X-CSRF-TOKEN`, which JavaScript can read. The frontend has to send the value of `X-CSRF-TOKEN` back in the `X-CSRF-TOKEN` header on the `[FromForm]` import endpoints. If it doesn't, those endpoints return **400** because antiforgery validation fails.

---

## `/user` — [UserEndpoints.cs](../../PlatinumCredentialManager.Api/Endpoints/UserEndpoints.cs)

### `GET /user/me`

| Status | Body |
| --- | --- |
| 200 | `UserProfile` |
| 401 | `ProblemDetails`, detail `"User authentication expired."` (no `sub` claim) |
| 404 | **empty**: the user row has not been provisioned yet |

### `POST /user/me`

Creates the user row the first time it is called, together with a default `"Miscallaneous"` category. The request has no body.

| Status | Body |
| --- | --- |
| 201 | `UserProfile` (first call; `Location: /user/me`) |
| 200 | `UserProfile` (user already existed, or lost a creation race) |
| 401 | `ProblemDetails`, detail `"User authentication expired."` |

```json
{
  "id": 3,
  "keycloakId": "8f1c...-uuid",
  "categories": [{ "id": 7, "categoryName": "Miscallaneous" }]
}
```

---

## `/category` — [CategoryEndpoints.cs](../../PlatinumCredentialManager.Api/Endpoints/CategoryEndpoints.cs)

### `GET /category`

| Status | Body |
| --- | --- |
| 200 | `CategoryListItem[]` |
| 401 | `ProblemDetails` |

```json
[{ "categoryName": "Miscallaneous", "id": 7 }, { "categoryName": "Work", "id": 9 }]
```

### `GET /category/export`

| Status | Body |
| --- | --- |
| 200 | `ExportCategory[]`. Includes **plaintext passwords** |
| 401 | `ProblemDetails` |

### `GET /category/{id}`

| Status | Body |
| --- | --- |
| 200 | `CategoryDetail` (credentials **without** passwords) |
| 401 | `ProblemDetails` |
| 404 | **empty** (the category doesn't exist, or the user doesn't own it) |

```json
{
  "id": 9,
  "categoryName": "Work",
  "credentials": [
    { "id": 12, "serviceName": "GitHub", "username": "rnguyen", "dateCreated": "2026-10-20", "dateLastUpdated": "2026-10-24" }
  ]
}
```

### `POST /category`

Request: `{ "categoryName": string }` (required, max 100 characters)

| Status | Body |
| --- | --- |
| 201 | `CategoryDetail` (`credentials` is always `[]`) |
| 400 | `ProblemDetails`: `"There exists a category that has the same name!"` or `"Category name cannot be named 'Miscallaneous'"` |
| 400 | `ValidationProblemDetails` (DTO validation) |
| 401 | `ProblemDetails` |

> ⚠️ `.WithName(GetCategoryEndpointName)` is attached to this POST route, not to `GET /category/{id}`. Because of that, `CreatedAtRoute` builds the `Location` header from the POST route (`/category?id=9`), not `/category/9`. The body is unaffected.

### `PUT /category/{id}`

Request: `{ "categoryName": string }` (required, max 100 characters)

| Status | Body |
| --- | --- |
| 204 | **empty** |
| 400 | **bare string**: `"Name cannot be zero legnth!"`, `"Category cannot be blank!"`, `"Category name cannot be named 'Miscallaneous'"`, or `"There exists a category that has the same name!"` |
| 400 | `ValidationProblemDetails` (DTO validation) |
| 401 | `ProblemDetails` |
| 404 | **empty** |

> ⚠️ Saving a category under its current name returns 400 "same name", because the duplicate check doesn't exclude the category being edited.

### `DELETE /category/{id}`

This moves the category's credentials into `Miscallaneous` and then deletes the category. If a moved credential's name clashes with one already there, it gets a ` - XXXXXX` suffix.

| Status | Body |
| --- | --- |
| 204 | **empty** |
| 400 | **bare string**: `"Cannot delete default category 'Miscallaneous!"` |
| 401 | `ProblemDetails` |
| 404 | `ProblemDetails`, detail `"Category not found."` |

### `POST /category/import`

Request: `multipart/form-data` with a `file` field. The CSV header is `Category Name, Service Name, Username, Password`. The `X-CSRF-TOKEN` header is required.

| Status | Body |
| --- | --- |
| 200 | **bare string**: `"CSV successfully imported!"` |
| 400 | `ImportProblem`, detail `"Empty file. Please fill out the file."` or `"Errors found in imported CSV."` |
| 400 | antiforgery failure (token missing or invalid) |
| 401 | `ProblemDetails` |

The error counters are flattened onto the root of the problem object. Every key is always present, and each value is an integer (`0` = no error of that kind):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "400 Bad Request",
  "status": 400,
  "detail": "Errors found in imported CSV.",
  "emptyFile": 0,
  "noCredentials": 0,
  "missingColumnError": 0,
  "tooManyColumnError": 0,
  "wrongColumnName": 0,
  "missingRowEntryError": 0,
  "tooManyRowEntriesError": 0,
  "noCategoryName": 0,
  "categoryNameTooLong": 0,
  "noServiceName": 0,
  "duplicateServiceName": 2,
  "serviceNameTooLong": 0,
  "usernameTooLong": 0,
  "noPassword": 1,
  "passwordTooLong": 0
}
```

> ⚠️ A data row with exactly **3** fields gets past the length check (`<= 2` / `> 4`). The code then reads `credential[3]`, which throws `IndexOutOfRangeException`, and the response is **500** instead of a 400 with `missingRowEntryError`.

---

## `/creds` — [CredentialEndpoints.cs](../../PlatinumCredentialManager.Api/Endpoints/CredentialEndpoints.cs)

### `GET /creds`

| Status | Body |
| --- | --- |
| 200 | `CredentialDetail[]`: every credential the user has, **with plaintext passwords** |
| 401 | `ProblemDetails` |

### `GET /creds/search`

Query parameters, bound from `SearchCredentialDto`:

| Param | Required | Notes |
| --- | --- | --- |
| `createUpdateChoice` | yes | `"Create"` filters on `dateCreated`; **any other value** filters on `dateLastUpdated` |
| `userDateChoice` | yes | `lastSevenDays` \| `lastThirtyDays` \| `lastYear` \| `boundedByDates`. Any other value means no date filter |
| `startDate` / `endDate` | no | only used with `boundedByDates`; parsed with `DateOnly.Parse`. Use `YYYY-MM-DD` |
| `categoryName` / `serviceName` | no | substring match |

| Status | Body |
| --- | --- |
| 200 | `CredentialSearchResult[]` (no username or password; the id field is **`credentialId`**) |
| 400 | a required query parameter is missing |
| 401 | `ProblemDetails` |
| 500 | `startDate`/`endDate` can't be parsed as a date (`DateOnly.Parse` throws) |

```json
[{ "credentialId": 12, "categoryId": 9, "categoryName": "Work", "serviceName": "GitHub", "dateCreated": "2026-10-20", "dateLastUpdated": "2026-10-24" }]
```

### `GET /creds/{id}`

| Status | Body |
| --- | --- |
| 200 | `CredentialDetail` (with password) |
| 401 | `ProblemDetails` |
| 404 | `ProblemDetails`, detail `"Credential not found."` or `"Category not found."` |

```json
{
  "id": 12,
  "serviceName": "GitHub",
  "username": "rnguyen",
  "password": "hunter2",
  "dateCreated": "2026-10-20",
  "dateLastUpdated": "2026-10-24",
  "categoryName": "Work",
  "categoryId": 9
}
```

### `POST /creds`

Request: `{ serviceName: string, username: string or null (optional), password: string, categoryId: number }`. Each string is at most 100 characters.

| Status | Body |
| --- | --- |
| 201 | `CredentialDetail` (`Location: /creds/{id}`) |
| 400 | `ValidationProblemDetails` (DTO validation) |
| 401 | `ProblemDetails` |
| 404 | `ProblemDetails`, detail `"Category does not exist."` |
| 409 | `ProblemDetails`, detail `"Service name already taken in selected category"` |

### `PUT /creds/{id}`

Request: `{ serviceName: string, username: string, password: string, categoryId: number }`. **All four fields are required**, `username` included. Send `""` when there's no username, or DTO validation rejects the request.

| Status | Body |
| --- | --- |
| 204 | **empty** |
| 400 | `ProblemDetails`: `"User cannot update a credential they do not own!"` (bad `categoryId`) or `"Password cannot be blank!"` |
| 400 | `ValidationProblemDetails` (DTO validation) |
| 401 | `ProblemDetails` |
| 404 | `ProblemDetails`, detail `"Credential not found."` |
| 409 | `ProblemDetails`, detail `"Service name already taken in selected category"` |

### `DELETE /creds/{id}`

| Status | Body |
| --- | --- |
| 204 | **empty**. Returned **even when the id doesn't exist** or belongs to another user (the delete simply affects 0 rows) |
| 401 | `ProblemDetails` |

### `POST /creds/import/{categoryId}`

Request: `multipart/form-data` with a `file` field. The CSV header is `Service Name, Username, Password`. The `X-CSRF-TOKEN` header is required.

| Status | Body |
| --- | --- |
| 200 | **bare string**: `"CSV successfully imported!"` |
| 400 | `ImportProblem`, detail `"Empty file. Please fill out the file."` or `"Errors found in imported CSV."` |
| 400 | antiforgery failure |
| 401 | `ProblemDetails` |

The error counter keys are the same as for `/category/import`, **without** `noCategoryName` and `categoryNameTooLong`:

`emptyFile`, `noCredentials`, `missingColumnError`, `tooManyColumnError`, `wrongColumnName`, `missingRowEntryError`, `tooManyRowEntriesError`, `noServiceName`, `duplicateServiceName`, `serviceNameTooLong`, `usernameTooLong`, `noPassword`, `passwordTooLong`

> ⚠️ The endpoint never checks that `categoryId` exists or belongs to the caller. With a bad id, validation passes, and `SaveChangesAsync` then fails on the foreign key → **500**.

---

## Quick reference

| Method | Route | Success | Success body |
| --- | --- | --- | --- |
| GET | `/antiforgery/token` | 200 | empty (sets cookies) |
| GET | `/user/me` | 200 | `UserProfile` |
| POST | `/user/me` | 201 / 200 | `UserProfile` |
| GET | `/category` | 200 | `CategoryListItem[]` |
| GET | `/category/export` | 200 | `ExportCategory[]` |
| GET | `/category/{id}` | 200 | `CategoryDetail` |
| POST | `/category` | 201 | `CategoryDetail` |
| PUT | `/category/{id}` | 204 | empty |
| DELETE | `/category/{id}` | 204 | empty |
| POST | `/category/import` | 200 | `"CSV successfully imported!"` |
| GET | `/creds` | 200 | `CredentialDetail[]` |
| GET | `/creds/search` | 200 | `CredentialSearchResult[]` |
| GET | `/creds/{id}` | 200 | `CredentialDetail` |
| POST | `/creds` | 201 | `CredentialDetail` |
| PUT | `/creds/{id}` | 204 | empty |
| DELETE | `/creds/{id}` | 204 | empty |
| POST | `/creds/import/{categoryId}` | 200 | `"CSV successfully imported!"` |
