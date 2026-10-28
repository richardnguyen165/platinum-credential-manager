# API Request Payloads

What the frontend sends to every endpoint that takes input, in two layers:

1. **What the API expects**: the C# request DTO for each endpoint (body, query string, or form).
2. **What the client hands to `serviceHandler`**: the `payload` argument for each `action`, and how it turns into the HTTP request.

This is the companion to [api-response-shapes.md](api-response-shapes.md), which covers what comes back.

Sources: [Dtos/](../../PlatinumCredentialManager.Api/Dtos/), [Endpoints/](../../PlatinumCredentialManager.Api/Endpoints/), [services/](../../PlatinumCredentialManager.Client/src/services/), [interceptors/](../../PlatinumCredentialManager.Client/src/interceptors/).

---

## Conventions that apply everywhere

- **JSON key casing:** ASP.NET's default web JSON settings read property names **case-insensitively**. `CategoryName`, `categoryName`, and `categoryname` all bind to the same DTO field. The client mixes casings (form fields are PascalCase, `categoryId` is camelCase), and that works.
- **Query string casing:** `[AsParameters]` binding also matches names case-insensitively, so `?ServiceName=...` binds to `serviceName`.
- **Unknown keys are ignored.** An extra field in a JSON body is silently dropped. It never causes a 400.
- **Missing keys:**
  - A missing **string** field arrives as `null`. If it's `[Required]`, DTO validation rejects it with a 400 `ValidationProblemDetails`.
  - A missing **`int`** field arrives as `0`. `[Required]` does **not** catch this, because `0` is a valid `int`. The handler then runs with `categoryId: 0`.
- **Length limits:** `[StringLength(100)]` means at most 100 characters. Validation runs **before** the handler.
- **Headers on every request:** the `http` axios instance sends `Content-Type: application/json`, `Authorization: Bearer <keycloak token>` (refreshed when it has fewer than 30 seconds left), and cookies (`withCredentials: true`).
- **Headers on form uploads:** the `sendingForm` instance sends `Content-Type: multipart/form-data`, plus `X-CSRF-TOKEN`. Before each upload it calls `GET /antiforgery/token` and copies the value of the `X-CSRF-TOKEN` cookie into the header.

---

## Request DTOs

These are the C# request DTOs, written as they should look on the wire. The names in bold are the labels the endpoint sections below use.

**CreateCategory** ([CreateCategoryDto.cs](../../PlatinumCredentialManager.Api/Dtos/Category/CreateCategoryDto.cs)): JSON body

```
{
  categoryName: string   // required, max 100
}
```

**UpdateCategory** ([UpdateCategoryDto.cs](../../PlatinumCredentialManager.Api/Dtos/Category/UpdateCategoryDto.cs)): JSON body

```
{
  categoryName: string   // required, max 100
}
```

**CreateCredential** ([CreateCredentialDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/CreateCredentialDto.cs)): JSON body

```
{
  serviceName: string,          // required, max 100
  username?: string | null,     // optional, max 100; stored as "" when null
  password: string,             // required, max 100
  categoryId: number            // "required", but a missing key becomes 0 (see conventions)
}
```

**UpdateCredential** ([UpdateCredentialDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/UpdateCredentialDto.cs)): JSON body. PUT is a full replace, so every field must be sent.

```
{
  serviceName: string,   // required, max 100
  username: string,      // required, max 100. Send "" for "no username", NOT null or a missing key
  password: string,      // required, max 100
  categoryId: number     // "required", but a missing key becomes 0
}
```

**SearchCredential** ([SearchCredentialDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/SearchCredentialDto.cs)): **query string**, not a body

```
?createUpdateChoice=<string>     // required. "Create" filters on dateCreated; anything else filters on dateLastUpdated
&userDateChoice=<string>         // required. lastSevenDays | lastThirtyDays | lastYear | boundedByDates
&startDate=<YYYY-MM-DD>          // optional; only read when userDateChoice=boundedByDates
&endDate=<YYYY-MM-DD>            // optional; only read when userDateChoice=boundedByDates
&categoryName=<string>           // optional, substring match
&serviceName=<string>            // optional, substring match
```

**ImportCategories** ([ImportCategoriesDto.cs](../../PlatinumCredentialManager.Api/Dtos/Category/ImportCategoriesDto.cs)): `multipart/form-data`

```
file: File   // required. CSV header: Category Name, Service Name, Username, Password
```

**ImportCredentials** ([ImportCredentialsDto.cs](../../PlatinumCredentialManager.Api/Dtos/Credential/ImportCredentialsDto.cs)): `multipart/form-data`

```
file: File   // required. CSV header: Service Name, Username, Password
```

---

## Endpoints that take input

| Method | Route | Route params | Input | Input type |
| --- | --- | --- | --- | --- |
| POST | `/category` | — | `CreateCategory` | JSON body |
| PUT | `/category/{id}` | `id: number` | `UpdateCategory` | JSON body |
| POST | `/category/import` | — | `ImportCategories` | form + `X-CSRF-TOKEN` |
| GET | `/creds/search` | — | `SearchCredential` | query string |
| POST | `/creds` | — | `CreateCredential` | JSON body |
| PUT | `/creds/{id}` | `id: number` | `UpdateCredential` | JSON body |
| POST | `/creds/import/{categoryId}` | `categoryId: number` | `ImportCredentials` | form + `X-CSRF-TOKEN` |

These endpoints take **no body**. At most they take an id in the route:

`POST /user/me`, `GET /user/me`, `GET /category`, `GET /category/export`, `GET /category/{id}`, `DELETE /category/{id}`, `GET /creds`, `GET /creds/{id}`, `DELETE /creds/{id}`, `GET /antiforgery/token`

### What a missing `categoryId` does

Because a missing `int` key becomes `0`, sending a credential body without `categoryId` does **not** produce a validation error. You get the handler's response for "category 0":

| Route | Result |
| --- | --- |
| `POST /creds` | 404 `ProblemDetails`, `"Category does not exist."` |
| `PUT /creds/{id}` | 400 `ProblemDetails`, `"User cannot update a credential they do not own!"` |

If you see one of these right after a create or edit, check whether `categoryId` actually made it into the request body.

---

## `serviceHandler(action, payload)`

`serviceHandler` is the single entry point that maps an `action` string to a service function. The `payload` shape depends on the action. There are three patterns:

- **Pass-through:** `payload` *is* the body or the query object.
- **Id-only:** `payload` is `{ id }`.
- **Id + data:** `payload` is `{ id, data }`. `id` goes into the URL, and `data` becomes the body (or the file).

### Action table

| `action` | `payload` shape | Service call | HTTP request |
| --- | --- | --- | --- |
| `get-user` | none | `getUser()` | `GET /user/me` |
| `post-new-user` | none | `postNewUser()` | `POST /user/me` (no body) |
| `create-category` | `CategoryForm` (pass-through) | `postCategory(payload)` | `POST /category`, body = `payload` |
| `get-category` | `{ id: number }` | `getCategory(id)` | `GET /category/{id}` |
| `edit-category` | `{ id: number, data: CategoryForm }` | `putCategory(id, data)` | `PUT /category/{id}`, body = `data` |
| `delete-category` | `{ id: number }` | `deleteCategory(id)` | `DELETE /category/{id}` |
| `export-categories` | none | `exportCategories()` | `GET /category/export` |
| `allCategories` | `File` (**the payload is the file itself**, not an object) | `importCategories(payload)` | `POST /category/import`, form `file` |
| `getAllCredentials` | none | `getAllCredentials()` | `GET /creds` |
| `get-credential` | `{ id: number }` | `getCredential(id)` | `GET /creds/{id}` |
| `create-credential` | `CredentialForm` (pass-through) | `postCredential(payload)` | `POST /creds`, body = `payload` |
| `edit-credential` | `{ id: number, data: CredentialForm }` | `putCredential(id, data)` | `PUT /creds/{id}`, body = `data` |
| `delete-credential` | `{ id: number }` | `deleteCredential(id)` | `DELETE /creds/{id}` |
| `search-credential` | `SearchQuery` (pass-through) | `searchCredential(payload)` | `GET /creds/search?…`, sent as axios `params` |
| `allCredentials` | `{ id: number, data: File }`. **`id` is the categoryId** | `importCredentials(data, id)` | `POST /creds/import/{id}`, form `file` |

> Note the argument order: `importCredentials` takes `(file, categoryId)`, while the `{ id, data }` payload lists `id` first. The handler swaps them for you.

### Client-side payload shapes

These are the objects the UI builds before calling `serviceHandler`. The keys come from the form inputs' `key` values, so they're **PascalCase**. `categoryId` is the exception: it's added in camelCase.

**CategoryForm**: used by `create-category` and `edit-category`

```
{
  CategoryName: string,
  categoryId?: number    // appended automatically when the modal is opened inside a category view;
                         // the server ignores it on both category routes
}
```

**CredentialForm**: used by `create-credential` and `edit-credential`

```
{
  ServiceName: string,
  Username: string,
  Password: string,
  categoryId: number     // create: injected from the current category.
                         // edit: set from the Category <select>
}
```

How it maps to the DTOs:

| Form key | DTO field | `CreateCredential` | `UpdateCredential` |
| --- | --- | --- | --- |
| `ServiceName` | `serviceName` | required | required |
| `Username` | `username` | optional (`null`/missing OK) | **required**, so `""` must be sent, not missing |
| `Password` | `password` | required | required |
| `categoryId` | `categoryId` | must be present (missing → `0`) | must be present (missing → `0`) |

**SearchQuery**: used by `search-credential`. It's built from the search form's reactive state and run through `parsePayload` first, which **drops every key whose value is falsy** (`""`, `null`, `undefined`, `0`, `false`).

```
{
  CreateUpdateChoice: "Create" | "Update",
  UserDateChoice: "lastSevenDays" | "lastThirtyDays" | "lastYear" | "boundedByDates",
  CategoryName?: string,   // omitted when blank
  ServiceName?: string,    // omitted when blank
  StartDate?: string,      // "YYYY-MM-DD"; only kept for boundedByDates
  EndDate?: string         // "YYYY-MM-DD"; only kept for boundedByDates
}
```

Example: axios serializes this into the query string as-is.

```
GET /creds/search?CategoryName=Work&UserDateChoice=boundedByDates&CreateUpdateChoice=Create&StartDate=2026-10-01&EndDate=2026-10-26
```

The same object is also pushed into the page URL (`router.push({ query: payload })`), so a search can be bookmarked and restored.

**Import payloads**: the file comes from the `<input type="file">`. The client rejects files over 5 MB before any request is sent.

```
allCategories:  File
allCredentials: { id: number /* categoryId */, data: File }
```

The service then wraps the file in `FormData` under the key `file`. That key matches the DTO's `File` property, case-insensitively.

---

## Checks to run in the Network tab

These are hints, not guaranteed bugs. Open devtools → Network, perform each action, and compare the **Request Payload** against the DTO tables above.

- **Edit a credential, change only one field, and save.** Are all four DTO fields present in the PUT body? Is `categoryId` there even when you didn't touch the dropdown?
- **Open an edit modal.** Are the inputs pre-filled with the current values? If they aren't, ask what that means for the body you send on save.
- **Create a credential and leave Username empty.** Is the key missing, `""`, or `null`? Each one is fine for POST, but is the same true for PUT?
- **Reload a bookmarked search URL that is missing some query keys.** Do the restored `CreateUpdateChoice` and `UserDateChoice` values look like valid options for *those* fields?
- **Trigger a failed search.** Is the toast built from the server's error response, or from something else?

---

## Quick reference

| `serviceHandler` action | Payload | → Endpoint | → DTO |
| --- | --- | --- | --- |
| `create-category` | `{ CategoryName }` | `POST /category` | `CreateCategoryDto` |
| `edit-category` | `{ id, data: { CategoryName } }` | `PUT /category/{id}` | `UpdateCategoryDto` |
| `allCategories` | `File` | `POST /category/import` | `ImportCategoriesDto` |
| `create-credential` | `{ ServiceName, Username, Password, categoryId }` | `POST /creds` | `CreateCredentialDto` |
| `edit-credential` | `{ id, data: { ServiceName, Username, Password, categoryId } }` | `PUT /creds/{id}` | `UpdateCredentialDto` |
| `allCredentials` | `{ id: categoryId, data: File }` | `POST /creds/import/{categoryId}` | `ImportCredentialsDto` |
| `search-credential` | `SearchQuery` | `GET /creds/search` | `SearchCredentialDto` (`[AsParameters]`) |
| `get-category` / `delete-category` | `{ id }` | `GET` / `DELETE /category/{id}` | — |
| `get-credential` / `delete-credential` | `{ id }` | `GET` / `DELETE /creds/{id}` | — |
| `get-user`, `post-new-user`, `export-categories`, `getAllCredentials` | none | — | — |
