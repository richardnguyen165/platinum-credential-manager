# Frontend organization: things to tidy before adding Pinia stores

> Notes from a review of the Vue frontend's structure (2026-10-21). Written as hints: each item
> describes the symptom, the concept, and what to check, not where it is or how to fix it.

Suggested order: 1 and 2 first (they change what everything else needs), then 3 and 4, then 5, and
the large components (6) last, since the earlier steps change what they have to do.

## 1. HTTP and auth logic is repeated

There are two request clients that each repeat the same steps: refresh the token before a request,
attach it, and log the user out on a 401. The service layer also has a place that logs out on
errors, and the router and the auth composable have their own login and logout calls.

- **Why it matters:** When behavior is copied, a fix lands in one copy and not the others. It is the
  same pattern as the CSV export notes (one path quotes, another doesn't).
- **Question:** What is truly shared between the clients, and what really differs (content type,
  CSRF handling)? Which single place should own the decision "this session is over, log out"?
- **What to check:** Search for every logout call and ask whether each one should be there. Does a
  401 always mean the session is over, or can it mean "not allowed"?
- [x] Listed every place that logs out or refreshes a token
- [x] Decided what is shared and what differs
- [x] Decided who owns logout
- [x] Fixed

## 2. What the store should own

A store exists but holds only three ids (user, category, credential), with a setter for each. It is
named after the user while also holding category and credential ids.

- **Concept:** A store earns its place when it holds data several parts of the app need and the
  actions that load it. A refresh wipes the store (it lives in memory), so anything that matters
  must be refetched. The URL survives a refresh; a store doesn't.
- **Questions:**
  - Which of the three ids is already in the route? Should it be read from there instead?
  - Is the user id needed in the browser at all, given the API works out the user from the token?
  - Is the credential id just a local selection (props, events or provide/inject) or shared state?
- **Also think about:**
  - Stores must wait until auth init has finished before fetching.
  - Stores should be cleared on logout so another user in the same tab can't see old data.
  - Reactivity is lost if state is destructured directly out of a store. Look up the helper for it.
  - Check the naming convention in the Pinia docs for store functions.
- [ ] Decided what belongs in the URL, a store, provide/inject, or props
- [ ] Decided what data (not just ids) a store should own
- [ ] Decided when stores reset

## 3. Folders blur their responsibilities

Some files in the services folder are not services, and related code is split between folders. CSV
export lives in utilities while CSV import filtering lives in services. Auth is spread across a
config file, a composable, the router and the HTTP clients.

- **Question:** In this app, what is a service, a utility, a composable, a component and a view?
  Which files don't fit their folder?
- **What to check:** Pick one feature (CSV import and export) and list every file it touches.
- [ ] Wrote down what each folder is for
- [ ] Moved files that don't fit

## 4. Where data is fetched

Some components may call the API directly and others go through the service layer.

- **Question:** What should the rule be? "Components never call the HTTP client directly" makes
  store work and testing much simpler.
- **What to check:** Search the components for direct imports of the HTTP clients.
- [ ] Decided on the rule
- [ ] Checked every component against it

## 5. Naming consistency

Several views, components and routes have similar or inconsistent names, and one has a spelling
that looks accidental. A few utility files are tiny.

- **Question:** Do file names, component names and route names tell the same story? Could a new
  reader guess what a file does from its name?
- [ ] Listed names that are unclear or inconsistent
- [ ] Renamed (and updated imports and routes)

## 6. Large components do several jobs

The form, the pagination grid and the modal are the biggest files in the project. Large files often
mix display, validation, data loading and submitting.

- **Concept:** Splitting by job (what it shows, what it validates, what it loads) tends to give
  smaller pieces that are easier to test, such as composables and child components.
- **What to check:** For each, write one sentence per job it does. If you need "and" several times,
  that is where to look for a split.
- [ ] Listed each large component's jobs
- [ ] Decided which jobs become composables or child components
- [ ] Split
