# Test CSVs for import

Two import modes, two sets of files:

| Folder | Import mode | Header |
|---|---|---|
| `*/credential/` | Import into one category | `Service Name,Username,Password` |
| `*/category/` | Import into any category | `Category Name,Service Name,Username,Password` |

## How to read the expected results

- **VALID** = HTTP 200 and the success toast. Rows are created.
- **REJECT** = HTTP 400. Nothing is saved. The response lists counters; only the non-zero ones are written below. Every counter not listed is `0`.
- **OBSERVE** = the expected result comes from reading the backend rules, but I'm less sure what really happens. Run it and write down what you see.
- Header problems are checked first. If the header has any error, row errors are not checked at all.
- A row error counts once per problem. One row can raise several counters, for example `,,` raises both `noServiceName` and `noPassword`.
- Fields are trimmed before checks, so whitespace-only counts as empty.
- Limit is 100 characters per field. 100 passes, 101 fails.
- Fully blank lines are skipped by the CSV parser.

## Valid files: credential import

All valid credential files use different service names, so you can import every one of them, one after another, into the SAME category without a duplicate conflict (checked with a script). The same holds for the valid category files within one account. Do not re-import the same file twice, since that is the duplicate case.

| File | Expected |
|---|---|
| valid-basic | VALID, 3 credentials |
| valid-single-row | VALID, 1 credential |
| valid-blank-username | VALID, 2 credentials with no username |
| valid-max-length-100 | VALID, 3 credentials, each with one 100-char field |
| valid-header-case-and-spacing | VALID. Header is case-insensitive and trimmed |
| valid-header-no-space | VALID. `ServiceName` is accepted |
| valid-whitespace-trimmed | VALID. Stored values have no leading/trailing spaces. The second row has a blank username |
| valid-quoted-fields | VALID. Commas and `""` escaped quotes inside quotes are kept: username `ali"ce`, password `pw,with,commas` |
| valid-case-sensitive-names | VALID, 3 credentials (`Gmail`, `gmail`, `GMAIL`). OBSERVE: names are compared case-sensitively |
| valid-unicode | VALID. Accents, Japanese and emoji round-trip unchanged |
| valid-special-char-passwords | VALID. Passwords stored as plain text, shown escaped (check no XSS and no SQL effects) |
| valid-crlf-line-endings | VALID, 2 credentials |
| valid-no-trailing-newline | VALID, 2 credentials |
| valid-blank-lines-skipped | VALID, 2 credentials |
| valid-utf8-bom | VALID, 2 credentials. First header is still recognised despite the BOM |
| valid-many-rows-300 | VALID, 300 credentials. Check speed and list pagination/scrolling |

## Invalid files: credential import

| File | Expected counters |
|---|---|
| invalid-empty-file | `emptyFile=1` |
| invalid-only-blank-lines | `emptyFile=1` (blank lines are skipped, so zero records) |
| invalid-headers-only | `noCredentials=1` |
| invalid-missing-header | `wrongColumnName=3` (first data row is read as the header) |
| invalid-wrong-headers | `wrongColumnName=3` |
| invalid-one-wrong-header | `wrongColumnName=1` |
| invalid-swapped-header-order | `wrongColumnName=2` |
| invalid-underscore-header | `wrongColumnName=1`. OBSERVE: the `_` handling in header matching may not do what it looks like it does |
| invalid-header-two-columns | `missingColumnError=1` |
| invalid-header-one-column | `missingColumnError=2` |
| invalid-category-headers-in-credential-import | `tooManyColumnError=1` |
| invalid-header-error-hides-row-errors | `wrongColumnName=1` only. Row errors below the header are NOT reported |
| invalid-empty-service-name | `noServiceName=1` |
| invalid-whitespace-service-name | `noServiceName=1` |
| invalid-empty-password | `noPassword=1` |
| invalid-whitespace-password | `noPassword=1` |
| invalid-duplicate-service-name | `duplicateServiceName=1` |
| invalid-duplicate-service-name-whitespace | `duplicateServiceName=1` (trimmed before comparing) |
| invalid-exact-duplicate-row | `duplicateServiceName=1`. Exact duplicates are rejected, not ignored |
| invalid-duplicate-empty-service-names | `noServiceName=2`, `duplicateServiceName=1`. OBSERVE: two empty names also count as duplicates of each other |
| invalid-service-name-too-long | `serviceNameTooLong=1` |
| invalid-username-too-long | `usernameTooLong=1` |
| invalid-password-too-long | `passwordTooLong=1` |
| invalid-row-too-few-columns | `missingRowEntryError=2` |
| invalid-row-too-many-columns | `tooManyRowEntriesError=1` |
| invalid-row-trailing-comma-no-password | `noPassword=1` (3 fields present, last is empty, so not a "missing column") |
| invalid-malformed-unclosed-quote | OBSERVE. Either a 400 with `missingRowEntryError=1`, or a 500 parse error. Check that the UI shows something sensible |
| invalid-multiple-errors | `noServiceName=1`, `noPassword=2`, `duplicateServiceName=1`, `missingRowEntryError=1`, `tooManyRowEntriesError=1`, `serviceNameTooLong=1` |

## Valid files: category import

| File | Expected |
|---|---|
| valid-basic | VALID. Creates categories Social (2) and Banking (1) |
| valid-single-row | VALID. Creates Shopping (1) |
| valid-blank-username | VALID, 2 credentials with no username |
| valid-merge-into-existing-category | Import AFTER valid-basic. VALID. Adds to the existing Social (2 new) and Banking (1 new). No duplicate categories are created |
| valid-same-service-different-categories | VALID. Same service name (`Dropbox`) in 3 different categories is allowed |
| valid-max-length-100 | VALID. 4 credentials, each with one 100-char field (category, service, username, password) |
| valid-new-category-gaming | VALID. Creates Gaming (4) |
| valid-new-category-streaming | VALID. Creates Streaming Services (4) |
| valid-header-case-and-spacing | VALID |
| valid-header-no-space | VALID. `CategoryName,ServiceName,...` accepted |
| valid-whitespace-trimmed | VALID. Both rows land in one `Social` category with trimmed values. Second row has a blank username |
| valid-quoted-fields | VALID. Category `Banks, Credit Unions` (comma kept), username `ali"ce` |
| valid-unicode | VALID. 3 categories with unicode names |
| valid-special-char-passwords | VALID. Same checks as the credential version |
| valid-crlf-line-endings | VALID, 2 credentials |
| valid-no-trailing-newline | VALID, 2 credentials |
| valid-blank-lines-skipped | VALID, 2 credentials |
| valid-utf8-bom | VALID, 2 credentials |
| valid-many-rows-300 | VALID. 300 credentials across 10 categories (Cat1..Cat10) |

## Invalid files: category import

| File | Expected counters |
|---|---|
| invalid-empty-file | `emptyFile=1` |
| invalid-only-blank-lines | `emptyFile=1` |
| invalid-headers-only | `noCredentials=1` |
| invalid-missing-header | `wrongColumnName=4` |
| invalid-wrong-headers | `wrongColumnName=4` |
| invalid-one-wrong-header | `wrongColumnName=1` |
| invalid-swapped-header-order | `wrongColumnName=2` |
| invalid-underscore-header | `wrongColumnName=2`. OBSERVE: same `_` question as in the credential import |
| invalid-header-three-columns | `missingColumnError=1` |
| invalid-header-one-column | `missingColumnError=3` |
| invalid-credential-headers-in-category-import | `missingColumnError=1` |
| invalid-header-five-columns | `tooManyColumnError=1` |
| invalid-header-error-hides-row-errors | `wrongColumnName=1` only |
| invalid-empty-category | `noCategoryName=1` |
| invalid-whitespace-category | `noCategoryName=1` |
| invalid-empty-service-name | `noServiceName=1` |
| invalid-whitespace-service-name | `noServiceName=1` |
| invalid-empty-password | `noPassword=1` |
| invalid-whitespace-password | `noPassword=1` |
| invalid-duplicate-service-in-category | `duplicateServiceName=1` |
| invalid-duplicate-service-in-category-whitespace | `duplicateServiceName=1` |
| invalid-exact-duplicate-row | `duplicateServiceName=1` |
| invalid-category-too-long | `categoryNameTooLong=1` |
| invalid-service-name-too-long | `serviceNameTooLong=1` |
| invalid-username-too-long | `usernameTooLong=1` |
| invalid-password-too-long | `passwordTooLong=1` |
| invalid-row-two-columns | `missingRowEntryError=2` |
| invalid-row-three-columns | Ideally `missingRowEntryError=1`. OBSERVE: this is the boundary case (3 of 4 fields). Check for a 500 instead of a 400 |
| invalid-row-five-columns | `tooManyRowEntriesError=1` |
| invalid-row-trailing-comma-no-password | `noPassword=1` |
| invalid-malformed-unclosed-quote | OBSERVE. Same as the credential version |
| invalid-multiple-errors | `noCategoryName=1`, `duplicateServiceName=1`, `noServiceName=1`, `noPassword=1`, `missingRowEntryError=1`, `tooManyRowEntriesError=1`, `categoryNameTooLong=1` |

## Cases that need database state (no file of their own)

- **Duplicate against existing data (credential import):** import `valid-basic`, then import it again into the same category. Expect `duplicateServiceName=3`.
- **Duplicate against existing data (category import):** same idea, expect `duplicateServiceName=3`.
- **Category scope:** import credential `valid-basic` into category A, then into category B. The second should succeed, because duplicate checks are per category.
- **Rollback on rejection:** import `invalid-multiple-errors`, then check no rows or categories were created, including the one valid row.
- **Account isolation:** an import by user 1 must not collide with user 2's data.

## UI things to check while testing

- Each 400 shows the "Invalid CSV import!" toast and the error list in the modal.
- Header-error files show only the header message. File-level errors (empty file) show the file message.
- For `*-headers-only`, check what the modal shows, since the counter is `noCredentials` and not one of the header or row counters.
- Plural wording for counts of 1 vs 2+. For extra and wrong header columns use `tooManyColumnError` (e.g. 5-column header) and `wrongColumnName` of 1 vs 3.
- Re-submitting a corrected file clears the old errors.
- Closing and reopening the modal clears the old errors.
