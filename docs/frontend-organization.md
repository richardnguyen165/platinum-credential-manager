# Frontend organization: things to tidy before reworking shared state

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

**Outcome (2026-10-21):** Both clients now share one set of request/response helpers, and only the
clients' 401 handling logs out (the extra check in the service layer is gone).

- **Lessons from the refactor:**
  - An interceptor needs a *function* Axios can call later, once per request. Parentheses or
    `await` hand over the *result* of calling it once at load time. That silently registered nothing,
    so requests went out without a token, got a 401 and logged the user out mid-login.
  - Creating a client needs the options *object* straight away. An `async` function returns a
    promise instead, so the client had no base URL and requests went to the dev server (404).
  - A logout fired while the login callback is still in the URL gets rejected by Keycloak (400),
    because the default post-logout redirect is the current URL, which still contains the login code.
- **Still open:**
  - When the token refresh fails, the request still goes out without a token. What does Axios do if
    a request interceptor throws or rejects instead of returning the config? Which behavior do you want?
  - The error-copying loop uses `in` on the response data. Is it safe when there is no response at
    all (API down, request cancelled)?

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
- **Findings (2026-10-21):**
  - **The store isn't used anywhere.** Nothing imports it, so none of its ids are ever set or read.
    You're designing from scratch. If the app works without it, which of those values does it
    actually need, and where does it get them now?
  - **There's already a hand-made store.** The auth composable keeps reactive state at module level
    (including the user id) that's shared by everything using it. So the real choice is: Pinia store,
    or module-level reactive state in a composable? Look up what Pinia adds (devtools, structure,
    resetting, testing).
  - **The URL already carries most ids.** Category and credential ids are route params passed to
    components as props, and every authenticated route also carries the user id.
    - Does the frontend use the user id from the URL for anything, given the API reads the user
      from the token?
    - What happens if you change the user id in the URL to another number? What *should* happen?
    - What would storing ids the URL already provides (and keeps after a refresh) gain you?
  - **The service facade's future depends on this section.** Once store actions exist, they may
    absorb it, use it, or shrink it to an error-shaping helper. Decide after the stores exist.
- **Direction (2026-10-21): provide/inject over a Pinia store.** After mapping the component tree
  and the props flow (see the props-flow notes in this folder), there is very little truly global
  state. Auth already has its own composable and search state lives in the URL. A store looks like
  overkill for now.
  - **Not a store doesn't automatically mean inject.** Props, or having a component read the value
    itself, are still valid. Inject only pays off when all three hold:
    - **One clear owner:** a single ancestor owns the value.
    - **Real depth:** several layers in between only pass it along without using it.
    - **Shared, not separate:** every descendant that injects it should see the *same* instance.
  - Run each value that's passed through more than one level against those three. Values that come
    from the URL fail the first test, since the router already owns them.
  - **Watch out for:**
    - Modal open/close state: is there a page where two separate forms live under one owner? What
      happens if they both inject the same state?
    - Provided data lives only as long as its provider. It is gone when the route changes. If some
      data turns out to be needed across routes, that is where a store would earn its place again.
    - Injected dependencies don't show in templates, so the flow is harder to trace than with props.
    - Typing: look up how injected values get a type and what the type is when nothing provides it.
  - The branch name still says Pinia. Keep it or rename it, but this note is the record of why the
    direction changed.
- **Next step: fill in this table**, adding real data (category list, current credential, user
  profile) as well as ids:

  | State | Where it comes from | Who needs it | Must survive a refresh? | So it belongs in |
  |---|---|---|---|---|
  | User id | Comes from url | All the components | | |
  | Category id | When clicking on a category | | | |
  | Credential id | When clicking on a credential | | | |
  | Category list | When clicking on creds homepage | | | |
  | Current credential | When clicking on a credential from home page | | | |
  | User profile | When user logs in | No component | | |

- [ ] Filled in the state table
- [x] Decided between a Pinia store and composable state (leaning provide/inject, see Direction)
- [ ] Ran each drilled value through the three inject tests
- [ ] Decided what belongs in the URL, provide/inject, or props
- [ ] Checked whether any data is needed across routes (the case that would bring a store back)
- [ ] Decided what happens to provided or composable state on logout

## 3. Folders blur their responsibilities

Some files in the services folder are not services, and related code is split between folders. CSV
export lives in utilities while CSV import filtering lives in services. Auth is spread across a
config file, a composable, the router and the HTTP clients.

- **Question:** In this app, what is a service, a utility, a composable, a component and a view?
  Which files don't fit their folder?
- **What to check:** Pick one feature (CSV import and export) and list every file it touches.
- **What each folder is for (2026-10-21):**
  - **Services:** one function per API operation on one resource, i.e. data access (like
    repositories or gateways). They fetch and send data, they don't decide things.
  - **Utilities:** pure helpers. Data in, data out, no app state, no HTTP, no side effects.
  - **Composables:** reusable Vue logic that uses reactivity or lifecycle.
  - **Config:** sets up an external library once.
  - **Interceptors:** the configured HTTP clients plus their shared request/response helpers
    (base URL, token, CSRF, 401 handling).
  - **Feature folders (CSV):** everything for one feature in one place.
- **Layers, top to bottom:** components → service facade → services → HTTP clients. Each layer talks
  only to the one below it.
  - The facade is a *facade* by role (one simple entry point), a *dispatcher* by mechanism (picks a
    service by action name), and an *adapter* in its error shaping (one consistent error format).
  - Patterns describe code, they aren't rules. One small file doing all three is fine. Split only
    when changing one job forces you to touch another, the action list gets hard to scan, or you
    can't test the error shaping on its own.
  - Decision: keep the facade in services for now, as the one exception there. Revisit after
    section 2.
  - Most *use-case* logic (the order of steps for "import a CSV" or "save a form") currently lives
    in the components. See section 6.
- **Still open:**
  - The folders now mix two styles: by type (services, utils, ...) and by feature (CSV). Write down
    the rule for when something gets a feature folder. Would auth (config, composable, router,
    interceptors) qualify?
  - Does the name "interceptors" describe a folder that holds the client instances themselves?
- [x] Wrote down what each folder is for
- [x] Moved files that don't fit (build passes; check login, a normal request and a form submission,
      and CSV import and export by hand)
- [ ] Decided the feature-folder rule

## 4. Where data is fetched

Some components may call the API directly and others go through the service layer.

- **Question:** What should the rule be? "Components never call the HTTP client directly" makes
  store work and testing much simpler.
- **What to check:** Search the components for direct imports of the HTTP clients.
- **Rule:** Components only call the service facade. Only services touch the HTTP clients.
- **Checked (2026-10-21):** No component or view imports an HTTP client, axios or a service directly.
  - One exception outside the components: the CSV export module uses the facade for one call but a
    service directly for another, so that call skips the facade's error shaping. Is that on purpose
    (does it need the raw response)? Does its error look like the others?
- [x] Decided on the rule
- [x] Checked every component against it

## 5. Naming consistency

Several views, components and routes have similar or inconsistent names, and one has a spelling
that looks accidental. A few utility files are tiny.

- **Question:** Do file names, component names and route names tell the same story? Could a new
  reader guess what a file does from its name?
- **Concept:** Vue's style guide rule "Multi-word component names" (Essential). Single-word names
  can clash with HTML elements: a capitalized tag is looked up as a component, a lowercase one is
  a native element. A modal had a capitalized form tag that resolved to no component.
- **Done so far (2026-10-22):** the form became ActionFormModal, the modal became BaseModal, the
  views got a "Credentials" prefix, and the single-category page got a clearer name.
- **First:** the router still imported two files by their old names after a rename. After every
  batch, reload each route (also by pasting the URL) to catch broken imports.
- **Still to rename:**
  - **Header:** single word. The convention for single-instance components is a "The" or "App"
    prefix.
  - **The single-credential page:** give it the same pattern as the single-category page. Both
    are routed pages, so they belong in views, not components.
  - **The pagination grid:** it also sorts, imports and exports CSV, navigates and shows a single
    credential. Rename it to match, or wait for the split in section 6.
  - **"Searchup":** in the view, the route and the location values. Not a standard word.
  - **The two HTTP clients in the interceptors folder:** they are clients, not just interceptors,
    and one reads like a verb phrase.
  - **The service facade:** "handler" is vague; the name should say it is the one entry point
    components call.
  - **The request/response helpers and the payload parser:** vague. Which request? Which payload?
  - **The loading functions:** several "...LoaderHelper" variants. Name them after what they load,
    with one pattern everywhere.
  - **The grid's location prop:** it is a mode switch, and its values mix styles.
  - **Router imports:** one view is imported under a different name than its file.
  - **Routes:** paths use a "cred-" prefix while files say "Credentials", and params are
    snake_case while the JavaScript is camelCase. Consider route names instead of built strings.
  - **The misspelled "miscellaneous" category:** it is a data value, not just a name. Check where
    it comes from (backend, database, seed data) and change both sides together.
  - **Typos in comments and messages** ("retrieve", "animates").
- **Imports:** normalize to the `@` alias. Decide whether same-folder `./` imports stay, write the
  rule here, and apply it everywhere. Run a build afterwards.
- [x] Listed names that are unclear or inconsistent
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
