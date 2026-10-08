# CSV import — plan

> Design note for the Stage VIII CSV import. Records the decisions made, the minimal version to
> build first, and what was deliberately left for later.

## Decisions

| Concern | Choice | Why |
|---|---|---|
| When to validate | Validate every row before saving anything | A bad row can never leave a half-finished import behind, and all errors can be shown at once. |
| Invalid rows | Reject the whole file (all-or-nothing) | Simplest to get right. The user fixes the file and re-uploads; no partial state to reason about. |
| Endpoints | One endpoint that validates, then saves only if everything is valid | A separate validate endpoint can't be trusted by the write step, so write would re-validate anyway. |
| Where validation runs | C# API | Only the API can see existing categories and duplicates, and it can't trust the client. Client checks are optional extras. |
| Parsing | CsvHelper (NuGet) | Handles quoted values, commas, quotes and line breaks inside values, which passwords often contain. |
| Saving | One EF Core save call | If the database rejects one row, none are saved. |

## Build first

1. **One endpoint** that accepts the CSV upload and reads it with CsvHelper.
2. **Check the headers.** If they're wrong, return one error and stop.
3. **Check each row** for required fields, collecting errors as row number plus message.
4. **If there are any errors,** save nothing and return the list.
5. **If there are none,** match each category name to an existing category (create it if needed)
   and save all the credentials in one save call.
6. **On the Vue side,** show either the error list or "Import successful".

Start with steps 1 and 2: upload a file and have the API correctly accept or reject the headers.

## Sending the file from Vue to the API

Files go over HTTP as **multipart/form-data**, not JSON.

1. **Pick the file in Vue.** An `<input>` of type file gives a `File` object in its change event. Keep the file itself in state, not its text.
2. **Package it.** Build a `FormData` object and append the file under a field name. The API uses that field name to find the file.
3. **Send it with axios.** Add a function to a service file that calls `http`, like the other services. The Bearer token interceptor still applies.
   - **Trap:** [http.js](../PlatinumCredentialManager.Client/src/services/http.js) sets a default `Content-Type` of `application/json` on every request. Look up what axios does with a `FormData` body when the content type says JSON. A multipart request needs a "boundary" in its content type, which the browser must set.
4. **Receive it in C#.** A minimal API parameter of type `IFormFile` receives the upload. Its parameter name relates to the `FormData` field name. Open a stream from it and pass that through a `StreamReader` to CsvHelper.
   - If the endpoint complains about **antiforgery**, look up why minimal APIs ask for it with form uploads and how to opt out for a token-authenticated API.

**Debugging:** in DevTools → Network, check the request's **Content-Type** header and **Payload**. If it says JSON or shows no file, the problem is on the Vue side.

## CsvHelper

Install it in the API project: `dotnet add package CsvHelper` (run inside `PlatinumCredentialManager.Api`). Docs: joshclose.github.io/CsvHelper, "Getting Started". Parts to read:

- Reading rows straight into a class whose properties match the headers.
- Connecting headers with spaces ("Category Name") to C# property names.
- Reading only the header row first (for step 2).
- What happens by default on bad data or missing fields (it throws), and whether to collect those into the error list instead.

## Deferred for later

Each of these can be added on top without rewriting the minimal version.

- **Preview / confirm step.** A dry run that shows "47 will be imported, 3 duplicates skipped" before saving. Only worth it if users would actually benefit from confirming.
- **Error types and summary counts.** Give each error a fixed type (an enum) alongside its message, then group and count by type (LINQ `GroupBy`). Count *before* truncating.
- **Truncating the error list.** Return the first N errors plus the total count ("Showing 50 of 1,240").
- **Input limits.** A maximum file size or row count.
- **Duplicate detection.** Skip duplicates rather than reject them, and report them separately from errors.
- **Fuzzy matching** of near-duplicate names. Probably out of scope; normalizing case and whitespace catches most cases.

## Things to remember

- **Whole-file checks first.** Missing or misspelled headers, an empty file, or a non-CSV file should produce one clear error, not hundreds of row errors.
- **Category names repeat on purpose.** Many rows sharing a category is normal. Create a new category once and reuse it for the other rows. The database has no unique index on category name, so it won't stop duplicates for you.
- **Name matching.** Decide whether "Email", "email" and " Email " count as the same category.
- **BOM.** Excel's "CSV UTF-8" adds a BOM. `StreamReader` strips it by default, but if the first header ever fails to match, check this first.
- **Match the export.** Import and export should agree on header names and quoting, so an exported file imports back unchanged.
- **Never echo passwords** in any response, including error or duplicate lists.
- **Frontend response shape.** The Vue side should be able to tell a single file-level error apart from a list of row errors.

## Open questions (for when duplicates are added)

- What counts as a duplicate: service name + username? Does capitalization or whitespace matter?
- Check only against the database, or also against earlier rows in the same file?
- Same service and username but a different password: skip it, or treat it as an update?
- Should the skipped-duplicates list say whether each one matched the database or an earlier row?
