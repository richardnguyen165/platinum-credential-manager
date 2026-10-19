# Frontend credential handling: open issues

> Notes from a review of how the frontend handles credentials (2026-10-19). Written as hints: each
> item describes the symptom, the concept, and how to reproduce it, not where it is or how to fix
> it.

Suggested order: 1 (quickest, most serious), then 3, 2, 4, and 5 whenever it's convenient.

## 1. Passwords printed to the browser console

In more than one place, the frontend logs a whole object that contains a decrypted password: once
when a single credential loads, and once when a create or edit form is submitted (the logged
payload includes the password just typed).

- **Why it matters:** Anyone with devtools open on that machine, a browser extension that reads the
  console, or a screen recording of a debugging session can see the password. It undoes the masked
  password field.
- **Reproduce:** Open devtools, open a credential, then edit it and save. Read the console.
- **What to check:** Search the frontend for every console log and ask: could this object ever hold
  a username or password?
- [ ] Found every log that can contain a credential value
- [ ] Fixed

## 2. Some exports fetch every password to export a few

Two export paths (one category, and search results) start from a list without passwords. To fill
them in, they request all of the user's credentials, passwords included, and match them up in the
browser.

- **Why it matters:** Exporting three credentials sends every password the user has over the
  network. Since database encryption, it also makes the backend decrypt every row.
- **Reproduce:** Open the Network tab, export a single category, and look at the requests and their
  responses.
- **Question:** Which side should build the data an export needs: the browser or the API? What
  would the API need to offer for the browser to ask for only what it exports?
- [ ] Reproduced
- [ ] Decided which side builds the export data
- [ ] Fixed

## 3. Special characters in exported values

### 3a. Double quotes inside a value

Most export paths wrap each field in double quotes, which handles commas inside a value. A value
that itself contains a double quote still breaks the row.

- **Reproduce:** Save a credential whose password contains `"` (for example `ab"cd`), export it,
  and open the file in Excel and in a text editor.
- **Concept:** Look up how the CSV format (RFC 4180) escapes a quote inside a quoted field.
- [ ] Reproduced
- [ ] Fixed in every export path

### 3b. One export path doesn't quote at all

One of the export paths joins the raw values with commas and no quotes.

- **Reproduce:** Export a **single** credential whose password or service name contains a comma.
  Check whether the columns shift.
- **Question:** Should every export path share one way of turning a value into a CSV field?
- [ ] Reproduced
- [ ] Fixed

### 3c. CSV (formula) injection

A value that starts with `=`, `+`, `-` or `@` can be treated as a formula when the file is opened
in Excel or another spreadsheet app. Passwords often start with these characters.

- **Reproduce:** Save a credential with the password `=1+1`, export it, open it in Excel, and see
  what the cell shows.
- **Concept:** Look up "CSV injection" (OWASP has a page on it) and the common defences.
- **Trade-off:** The defence changes the exported value. Think about what that means when the same
  file is imported back into this app.
- [ ] Reproduced
- [ ] Decided on a defence
- [ ] Checked that the import still round-trips

## 4. Bugs in the export code

### 4a. Error path uses a variable before it exists

In two export paths, when the "get all credentials" request fails, the error message refers to a
variable that is only declared further down in the same function. Instead of showing the intended
alert, the code throws a different error.

- **Reproduce:** Stop the API (or make that request fail some other way), then try exporting one
  category and exporting search results. Watch the console.
- **Concept:** JavaScript's "temporal dead zone" for `let` and `const`.
- **Question:** Which object actually holds the error details at that point?
- [ ] Reproduced
- [ ] Fixed in both paths

### 4b. Cancelling the save dialog

When the browser supports the save-file picker and the user presses Cancel, the cleanup step still
runs on something that was never created.

- **Reproduce:** In Chrome or Edge, start any export and press Cancel in the save dialog. Watch the
  console for an error after the "Cancelling export" toast.
- **Concept:** A `finally` block runs on every path out of `try`, including an early `return`.
- **Question:** What is the value of the thing being closed if the picker never returned a file?
- [ ] Reproduced
- [ ] Fixed

## 5. An input in a modal may not update as you type

One input in a modal component has an attribute that looks like an event binding, but one character
differs from the syntax Vue uses for event bindings. Vue treats it as a plain attribute, so the
handler never runs.

- **Reproduce:** Open each modal that has text inputs, type into them, and check whether the value
  the modal submits matches what you typed (the Vue devtools component inspector helps here).
- **Concept:** The difference between a Vue directive (`v-on:` / its shorthand) and an ordinary
  HTML attribute.
- [ ] Found the input
- [ ] Fixed
