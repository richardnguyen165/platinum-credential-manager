# TypeScript Types: A Guide Through Our Codebase

As of 2026-11-22 · TypeScript 6.0.3 · vue-tsc 3.3

This guide explains the TypeScript features used in the client. Every example comes from code in `PlatinumCredentialManager.Client/src`, mostly [filterImportCSVResponse.ts](../PlatinumCredentialManager.Client/src/csv/filterImportCSVResponse.ts) and [serviceHandler.ts](../PlatinumCredentialManager.Client/src/services/serviceHandler.ts).

---

## 1. What TypeScript is

TypeScript is JavaScript plus type annotations. Any valid JavaScript is valid TypeScript. Before the code runs, the type annotations are **erased**, so nothing about types exists in the browser.

That has two consequences:

- **Types catch mistakes before you run the code.** For example, passing `"allCategory"` where `"allCategories"` was expected, or using a value that might be `undefined`.
- **Types never change runtime behaviour.** A type cannot validate what the API actually sends back. If the server returns something different from `ImportProblem`, TypeScript won't notice. It trusts what you told it.

In this project:

| Tool | What it does |
|---|---|
| `vite` | Runs and builds the app. It strips types but **does not check them**. |
| `vue-tsc --noEmit` (`npm run type-check`) | Checks types in `.ts` files and inside `<script lang="ts">` blocks of `.vue` files. It produces no output files. |
| `tsconfig.json` | Configures the checker. We use `strict: true`. `noUncheckedIndexedAccess` and `exactOptionalPropertyTypes` are currently commented out, to be re-enabled as a dedicated pass once the JS to TS conversion has landed. |

So the app can run fine while `type-check` still reports errors. Run `type-check` before you commit.

---

## 2. How the types work

### 2.1 Primitives

```ts
let count: number = 3;
let title: string = "Invalid CSV";
let valid: boolean = true;
let nothing: undefined = undefined;
let empty: null = null;
```

### 2.2 Type aliases and interfaces

Both give a name to the shape of an object.

```ts
// type alias (types/serviceType.ts)
export type ErrorData = {
    status: number | undefined,
    title: string | undefined,
    detail: string | null | undefined,
    extensions: Partial<Record<string, number>>
}

// interface (types/responseType.ts)
export interface ImportProblem {
    type: string,
    title: string,
    status: number,
    emptyFile: number,
    missingColumnError: number,
    // ...
}
```

For plain object shapes they are almost interchangeable. Use `type` when you need unions, `Extract`, indexed access and similar operations, because only `type` can do those.

### 2.3 Union types: "one of these"

`|` means "either". Anything typed `string | undefined` may hold either one, so you have to handle both cases (see **Narrowing**).

```ts
status: number | undefined
```

### 2.4 Literal types: "exactly this value"

A literal type is a single specific value. A union of literal types works like an enum:

```ts
// types/formType.ts
export type FormAction =
  | "create-category" | "edit-category" | "delete-category"
  | "create-credential" | "edit-credential" | "delete-credential"
  | "allCategories" | "allCredentials";
```

`const a: FormAction = "allCategory"` is an error because of the typo. A plain `string` would accept it.

### 2.5 Arrays

```ts
const headerErrors: string[] = [];   // same as Array<string>
```

### 2.6 Generics: types with parameters

A generic type takes another type as a parameter, the same way a function takes an argument. `HandlerResult` is generic over the success data `T`:

```ts
export type HandlerResult<T = unknown> =
    | { valid: true,  data: T }
    | { valid: false, data: ErrorData | null }
```

`HandlerResult<CategoryDetail>` means "on success, `data` is a `CategoryDetail`". `= unknown` is the default when no type is given.

### 2.7 `Record<K, V>`: an object used as a map

`Record<string, number>` means "an object whose keys are strings and whose values are numbers".

```ts
const counts: Record<string, number> = { emptyFile: 0, noPassword: 2 };
```

### 2.8 `keyof`: the names of an object's properties

`keyof ImportProblem` is the union of all of its property names:

```ts
keyof ImportProblem
// = "type" | "title" | "status" | "detail" | "emptyFile" | "noCredentials" | ...
```

### 2.9 Indexed access: `T["prop"]`

Square brackets on a **type** look up the type of one of its properties:

```ts
type ImportExtensions = ErrorData["extensions"];
// = Partial<Record<string, number>>
```

If `ErrorData.extensions` ever changes, `ImportExtensions` follows automatically. That's better than copying the type by hand.

`serviceHandler` uses the same idea to pick the return type for each action:

```ts
ResponseFor[C["action"]]
// "get-category" → CategoryDetail, "export-categories" → ExportCategory[], ...
```

### 2.10 `Extract<Union, Subset>`: keep part of a union

```ts
type ImportAction = Extract<FormAction, "allCategories" | "allCredentials">;
// = "allCategories" | "allCredentials"
```

`Extract` keeps only the members you list that really exist in `FormAction`. If `FormAction` is ever renamed, `ImportAction` breaks visibly instead of silently drifting. The opposite is `Exclude<Union, Subset>`.

### 2.11 `typeof` on a value

In a type position, `typeof x` means "the type of the variable `x`". It's how you turn a constant array into a type (see section 3.4).

### 2.12 Function signatures

```ts
function headerErrorHelper(headerError: HeaderErrorName, response: ImportExtensions): string { ... }
//                         ^ parameter types                                          ^ return type
```

Under `strict: true`, a parameter without a type is an **implicit `any`** error. `any` switches type checking off for that value, so strict mode makes you name the type.

### 2.13 Overloads (`serviceHandler`)

```ts
// What callers see:
export function serviceHandler<C extends ServiceCall>(serviceCall: C): Promise<HandlerResult<ResponseFor[C["action"]]>>;
// The real implementation, which is looser inside:
export async function serviceHandler(serviceCall: ServiceCall): Promise<HandlerResult<unknown>> { ... }
```

The first line is the **public signature**. Callers get a precise return type based on which action they pass. The second line is the body, which only has to satisfy a looser type.

---

## 3. `as const`, `readonly` and `satisfies`

These three work together in `filterImportCSVResponse.ts` and `serviceHandler.ts`:

```ts
const HEADER_ERROR_NAMES = [
    "missingColumnError",
    "tooManyColumnError",
    "wrongColumnName"
] as const satisfies readonly (keyof ImportProblem)[];
```

### 3.1 Without anything

```ts
const names = ["missingColumnError", "tooManyColumnError"];
// type: string[]
```

TypeScript **widens** the strings to `string`, so the specific names are lost. `names.push("anything")` is allowed.

### 3.2 `as const`

```ts
const names = ["missingColumnError", "tooManyColumnError"] as const;
// type: readonly ["missingColumnError", "tooManyColumnError"]
```

`as const` tells TypeScript to infer the narrowest possible type:

- Each string stays a **literal type**, not `string`.
- The array becomes a **`readonly` tuple**: a fixed length in a fixed order.

### 3.3 `readonly`

`readonly` means you can't change it through this reference. `push`, `pop`, `splice` and assigning to an index are all compile errors:

```ts
HEADER_ERROR_NAMES.push("extra");  // Error: Property 'push' does not exist on type 'readonly [...]'
```

Like every type, it's **compile-time only**. Use `Object.freeze` if you need runtime protection. `readonly` also works on object properties: `{ readonly id: number }`.

Why `satisfies readonly (keyof ImportProblem)[]` needs the word `readonly`: a `readonly` array can't be assigned to a mutable array type. Without it, the check would fail even with correct names.

### 3.4 `satisfies`

`satisfies` checks that the value fits a type **without changing the value's inferred type**.

```ts
] as const satisfies readonly (keyof ImportProblem)[];
```

- **Check:** every string must be a real property name of `ImportProblem`. A typo like `"wrongColumName"` is an error.
- **Keep:** the variable's type stays the precise tuple from `as const`. It does not become the wider `(keyof ImportProblem)[]`.

Compare it with a type annotation:

| Code | Is it checked? | Resulting type |
|---|---|---|
| `const x: (keyof ImportProblem)[] = [...]` | Yes | `(keyof ImportProblem)[]`, so the exact names are lost |
| `const x = [...] as const` | No | The exact tuple |
| `const x = [...] as const satisfies readonly (keyof ImportProblem)[]` | Yes | The exact tuple |

### 3.5 Turning the array into a union: `typeof X[number]`

```ts
type HeaderErrorName = typeof HEADER_ERROR_NAMES[number];
// = "missingColumnError" | "tooManyColumnError" | "wrongColumnName"
```

Read it inside out:

1. `typeof HEADER_ERROR_NAMES` gives the tuple type.
2. `[number]` means "the type of an element at any numeric index", which is the union of all elements.

The array is now the **single source of truth**. Add a name to the array and the type updates with it.

This also explains why `rowErrorHelper` can take `rowError: RowErrorName`, a precise union, instead of `string`.

---

## 4. `Partial<T>`

`Partial<T>` makes **every property optional**:

```ts
type Partial<T> = { [K in keyof T]?: T[K] };

Partial<{ a: number, b: string }>
// = { a?: number, b?: string }
```

An optional property may be missing, so reading it gives `number | undefined`.

### 4.1 Why `ErrorData.extensions` is `Partial`

```ts
extensions: Partial<Record<string, number>>
```

`serviceHandler` builds this object starting empty (`extensions: {}`). It only copies a count across when the server actually sent it:

```ts
const count = raw[name];
if (count !== undefined) currentErrorObject.extensions[name] = count;
```

So any key may be missing, and the type has to say so. `Partial` describes that honestly.

### 4.2 `noUncheckedIndexedAccess`

Our `tsconfig.json` turns this option on. With it, **any** lookup by a dynamic key (`obj[key]`, `arr[i]`) includes `| undefined`, even without `Partial`. The reason is that TypeScript can't know the key exists. Between `Partial` and this flag, `response[name]` is always `number | undefined`.

### 4.3 Dealing with it: `??`

The nullish coalescing operator `a ?? b` gives `b` when `a` is `null` or `undefined`, and `a` otherwise:

```ts
function errorCount(response: ImportExtensions, name: keyof ImportProblem): number {
    return response[name] ?? 0;   // number | undefined  →  number
}
```

Use `??` rather than `||`. `0 || 5` is `5`, because `0` is falsy, but `0 ?? 5` is `0`. For counts, `0` is a real value that you want to keep.

### 4.4 Related utility types

| Utility | What it does |
|---|---|
| `Partial<T>` | Every property becomes optional |
| `Required<T>` | Every property becomes required |
| `Readonly<T>` | Every property becomes `readonly` |
| `Pick<T, K>` | Keep only properties `K` |
| `Omit<T, K>` | Drop properties `K` |
| `Record<K, V>` | An object with keys `K` and values `V` |
| `Extract<U, X>` / `Exclude<U, X>` | Keep / remove union members |

---

## 5. Narrowing

A union type says what a value **might** be. **Narrowing** is TypeScript following your `if`, `switch` and `return` statements to work out what the value **must** be at a given line. After a check, the type inside that branch is smaller.

### 5.1 Equality checks

```ts
const count: number | undefined = response[name];
if (count === undefined) return "";
count;            // number: undefined was ruled out above
```

### 5.2 `typeof` checks

```ts
function show(x: string | number) {
    if (typeof x === "string") x.toUpperCase();  // x: string
    else x.toFixed(2);                           // x: number
}
```

### 5.3 Truthiness

```ts
if (response.data) {
    response.data.extensions;   // data: ErrorData (null ruled out)
}
```

Be careful with numbers here. `if (count)` also rules out `0`, which is usually not what you want for counts.

### 5.4 `switch` and literal comparisons

In `ActionFormModal.vue`, `action` is a `string`. Inside a case, TypeScript knows it's that exact value:

```ts
switch (action) {
    case "allCategories":
    case "allCredentials":
        // action: "allCategories" | "allCredentials"
        filterImportCSVResponse(action, ...);   // OK: matches ImportAction
}
```

The same thing happens with `if (rowError === "noCredentials")`: inside that branch, `rowError` has the type `"noCredentials"`.

### 5.5 Discriminated unions: the `valid` flag

`HandlerResult` is a **discriminated union**. Each member has a `valid` property with a different literal value. Checking that property tells TypeScript which member you have:

```ts
const response = await serviceHandler(payload);   // HandlerResult<...>

if (!response.valid) {
    response.data;     // ErrorData | null
} else {
    response.data;     // the success type T
}
```

This is the most useful pattern in the codebase. It's why `response.data.extensions` only works on the failure branch.

### 5.6 The `in` operator

```ts
if ("extensions" in obj) { ... }   // narrows which union member obj is
```

`in` narrows the **object's** type when the object is a union of shapes. It does **not** turn the property's value from `number | undefined` into `number`.

### 5.7 What does NOT narrow: `Object.hasOwn`

This was the error in `rowErrorHelper`:

```ts
if (rowError === "missingRowEntryError" && Object.hasOwn(response, rowError)) {
    response[rowError] > 1;   // Error: possibly 'undefined'
}
```

TypeScript's library declares `Object.hasOwn` as:

```ts
hasOwn(o: object, v: PropertyKey): boolean;
```

It returns a plain `boolean`, not a **type predicate**, so TypeScript learns nothing from it. Also, a key that exists can still hold the value `undefined`, so "the key exists" doesn't prove "the value is a number".

### 5.8 The reliable fix: copy into a local `const`

Narrowing works best on a local variable. Read the value once, handle `undefined`, then use the variable:

```ts
const count = errorCount(response, rowError);   // number (?? 0 inside)
const plural = count > 1 ? 's' : '';
```

or:

```ts
const count = response[rowError];
if (count === undefined) return "";
count > 1;   // count: number
```

Newer TypeScript (5.5 and later) can sometimes narrow `response[rowError]` directly when neither `response` nor `rowError` is ever reassigned. A local `const` is clearer, works in every case, and avoids repeating the lookup.

### 5.9 Writing your own narrowing: type predicates

A function that returns `x is T` teaches TypeScript a new check:

```ts
function isImportAction(a: string): a is ImportAction {
    return a === "allCategories" || a === "allCredentials";
}

if (isImportAction(action)) {
    filterImportCSVResponse(action, ...);   // action: ImportAction
}
```

TypeScript believes the predicate. If the function body is wrong, the types will be wrong too.

---

## 6. Mistakes we hit, and how to recognise them

Every entry here is a real error from converting the client to TypeScript. The error code is the fastest way back to the cause, so they are listed by code first.

### 6.0 Error code lookup

| Code | Message shape | Usually means |
|---|---|---|
| `TS2307` | Cannot find module `'@/utils/sort'` | Missing `.js` on the import (6.7) |
| `TS2322` | Type `X` is not assignable to type `Y` | Wrong type on either side; check the *declaration*, not the line reported. Inside a narrowed branch, two values that cannot be correlated (6.12) |
| `TS2339` | Property `x` does not exist on type `{}` | Declared empty: `reactive({})`, `ref({})` (6.1) |
| `TS2345` | Argument of type `X` not assignable to parameter `Y` | A prop or model still optional (6.8) |
| `TS2367` | This comparison appears to be unintentional | A union is missing a value the code really produces (6.4) |
| `TS2538` | Type `undefined` cannot be used as an index type | Indexing with an optional key |
| `TS2554` | Expected 1 arguments, but got 5 | A signature changed and a caller was missed |
| `TS2696` | The `Object` type is assignable to very few other types | Capital-`O` `Object` used as a type (6.2) |
| `TS2741` | Property `x` is missing in type ... but required | A type gained a field the real data does not have (6.4) |
| `TS7006` | Parameter implicitly has an `any` type | Untyped function parameter |
| `TS7053` | Element implicitly has an `any` type because expression of type `string` can't be used to index | Indexing `{}` or a generic `T` (6.1, 6.10) |
| `TS18046` | `x` is of type `unknown` | Runtime `defineProps({ x: Array })` — element type unknown (6.8) |
| `TS18047` / `TS18048` | `x` is possibly `null` / `undefined` | Optional prop or model (6.8) |

---

### 6.1 Empty values infer empty types

The single most common mistake. When you initialise with nothing, TypeScript concludes the type *is* nothing.

| You wrote | TypeScript inferred | What breaks |
|---|---|---|
| `reactive({})` | `{}` — object with **no** properties | `currentEntries.CategoryName` → `TS2339` |
| `ref({})` | `Ref<{}>` | `data.value[key] = ...` → `TS7053` |
| `ref(null)` | `Ref<null>` | `isMiscallaneous.value = true` → `TS2322` |
| `ref([])` | `Ref<never[]>` | `allData.value = rows` → `TS2322` |

`{}` and `[]` are not "some object" and "some array". In a **type** position:

- `{}` means an object with no known properties
- `[]` means a tuple of length exactly zero

That second one produces the giveaway message:

```
Type 'CategorySummary[]' is not assignable to type '[]'.
  Target allows only 0 element(s) but source may have more.
```

**Fix: put the type where the value is created.**

```ts
const currentEntries  = reactive<CategoryForm>({ CategoryName: "" });
const data            = ref<Record<string, unknown>>({});
const isMiscallaneous = ref(false);
```

Related: `Ref<T, S>` takes **two** parameters, and the second is the *setter* type — what you may assign to `.value`. `Ref<CategorySummary[], []>` therefore reads as `CategorySummary[]` but accepts only the empty tuple. Omit the second parameter unless the read and write types genuinely differ.

---

### 6.2 Capital-letter types are wrapper objects

`String`, `Boolean`, `Number` and `Object` are JavaScript's wrapper constructors. As **types** they mean the boxed object, not the primitive.

```ts
function todaysDate(): String { ... }   // breaks :max="todaysDate()"
function todaysDate(): string { ... }   // correct
```

`Object` is the worst of them — it means "any non-null value", so every property access fails, and TypeScript says so outright:

```
TS2696: The 'Object' type is assignable to very few other types.
        Did you mean to use the 'any' type instead?
```

Confusingly, the error often appears **where the value is used**, not where it was declared: `todaysDate(): String` on line 108 was reported as errors on lines 34 and 42, inside the template bindings.

---

### 6.3 Types and values are separate worlds

A type exists only during checking; `import type` is erased before the code runs. So a type can never appear where JavaScript will actually evaluate something.

```ts
defineProps({ rules: { type: Rule[] } })   // TS1011 — Rule[] is a type
defineProps<{ rules: Rule[] }>()           // correct, type position
```

In runtime `defineProps({...})`, `type:` wants a **constructor** (`Array`, `String`). In type-based `defineProps<{...}>()`, you want **types** (`Input[]`, `boolean`). Mixing the two forms produces errors in both directions — `Rule[]` where `Array` belongs, or `Object` where `Input[]` belongs.

To bridge them deliberately, use `PropType`:

```ts
defineProps({ rules: { type: Array as PropType<Rule[]>, required: true } })
```

---

### 6.4 Hand-written unions drift from reality

This bit us three times. A union gets written once and the code moves on without it.

| Union | Problem | Symptom |
|---|---|---|
| `UserDateChoice` | missing `"noBounds"` | `<option value="noBounds">` existed and worked; validating against the incomplete list silently rewrote a bookmarked search to `"lastSevenDays"` |
| `Input["type"]` | missing `"file"` | `TS2367` x3 — `inputs[0].type === 'file'` could never be true, so the CSV branch was dead |
| `Rule` | gained `key` it should not have | `TS2741` x17 — all 17 real rules lacked the field |

`TS2367` ("this comparison appears to be unintentional") is worth learning: it means the comparison can **never** be true, so the branch is dead code. Either the union is wrong or the comparison is.

**Fix: derive the type from one list instead of writing it twice.**

```ts
export const DATE_CHOICES = ["lastSevenDays", "lastThirtyDays", "lastYear",
                             "noBounds", "boundedByDates"] as const;
export type DateChoice = typeof DATE_CHOICES[number];
```

Note: with `verbatimModuleSyntax`, a `const` is a value, so it cannot ride along in an `import type` — it needs its own plain `import`.

---

### 6.5 `??` only removes `null` and `undefined`

Not "falsy", and not array wrappers. Two failures came from assuming more.

**It does not preserve `0`:**

```ts
let preserveZero = value ?? false;      // value = 0  ->  0 ?? false  ->  0
if (preserveZero || value) { ... }      // 0 || 0 -> falsy -> dropped anyway
```

The whole condition collapsed to `if (value)`. To mean what the comment claimed, name the cases:

```ts
if (value !== null && value !== undefined && value !== "")
```

**It does not unwrap an array:**

```ts
route.query.X ?? ""   // still string | LocationQueryValue[]
```

An array is not nullish, so `??` never fires on it. See 6.6.

---

### 6.6 URL query values are three things at once

```ts
type LocationQueryValue = string | null;
type LocationQuery = Record<string, LocationQueryValue | LocationQueryValue[]>;
```

- `null` — `"?x"` with no `=`
- `string[]` — `"?x=a&x=b"` repeats the key
- `undefined` — the key is absent

Collapse both shapes first, then check once — otherwise validation only guards the rare repeated case and lets `?UserDateChoice=banana` straight through:

```ts
const element = Array.isArray(query) ? query[0] : query;
if (element == undefined) return alternative;    // loose == catches null AND undefined
```

What **does not** work:

| Attempt | Why not |
|---|---|
| `String(x)` | `null` becomes the literal text `"null"` |
| `x?.toString()` | `["a","b"]` becomes `"a,b"` — merges values |
| `[x].flat()[0] ?? ""` | works, and picks the first value |

---

### 6.7 Imports need `.js` under `module: nodenext`

```ts
import { sortDecider } from "@/utils/sort";      // TS2307
import { sortDecider } from "@/utils/sort.js";   // correct
```

The file is `sort.ts`, but the extension you write is the **output** extension.

**This one matters more than it looks.** An unresolved module makes everything imported from it `any`, which *suppresses* downstream errors. Fixing 5 such imports took the project from 128 errors to 132 — the extra 9 were real mismatches (`SortAction`, `DataRow[][]`, `FormAction`) that had been hidden. Fix `TS2307` before trusting any error count.

---

### 6.8 Props and models are optional until you say otherwise

```ts
defineProps({ query: Object })        // Record<string, any> | undefined
defineModel<string>('errorMessage')   // ModelRef<string | undefined>
```

That `| undefined` caused most of the `TS18046` / `TS18047` / `TS18048` noise. It is usually not a real state — `router/index.ts` always passes `query`, and `BaseModal` always binds all four models.

```ts
defineProps({ query: { type: Object, required: true } })
defineModel<string>('errorMessage', { required: true })
defineProps<{ inputs: Input[] }>()    // type-based, no `?` means required
```

Converting to type-based `defineProps<{...}>()` removed 15 errors from `BaseModal.vue` in one edit.

Also: the runtime form loses element types. `rules: Array` gives `unknown[]`, so `rule.text` is `TS18046`. `defineProps<{ rules: Rule[] }>()` does not.

---

### 6.9 Generic inference needs `as const` when the target is optional

```ts
cleanAppSearchUpQuery(route.query.X, "Create", ['Create', 'Update'])
```

Assigned to a `const a: "Create" | "Update"` this infers `T` correctly. Assigned to `data["CreateUpdateChoice"]` on a `Partial<SearchQuery>` — type `"Create" | "Update" | undefined` — the `| undefined` breaks inference and `T` falls back to `string`.

```ts
..., ['Create', 'Update'] as const);   // correct
```

Separately, `.includes()` **does not narrow**. It returns a plain `boolean`, which carries no information about what was proven:

```ts
if (DATE_CHOICES.includes(s as DateChoice)) {
    const narrowed: DateChoice = s;   // s is still string
}
```

Widen the **array**, never the value — casting the array is true, casting the value is the thing you are trying to find out:

```ts
(DATE_CHOICES as readonly string[]).includes(s)       // honest cast
DATE_CHOICES.some(choice => choice === s)             // no cast at all
```

For real narrowing, write a predicate (see 5.9).

---

### 6.10 `Object.entries` throws away key types

```ts
for (const [key, value] of Object.entries(data)) {
    polishedData[key] = value;    // TS7053
}
```

`Object.entries` is typed `[string, any][]`, so `key` is a plain `string` and cannot index a generic `Partial<T>`. Iterate the keys instead, so the value keeps its real type:

```ts
for (const key of Object.keys(data) as (keyof T)[]) {
    const value = data[key];
}
```

Some assertion is unavoidable here — TypeScript cannot correlate a key variable with its value type across a loop. Keep the lie small and local.

---

### 6.11 Bugs the compiler could not see

Type-checking does not catch everything. These were found by reading:

| Bug | Where |
|---|---|
| `data[input.key]` missing `.value` — form values written onto the Ref wrapper, never into `data.value` | `BaseModal.vue` |
| `router.push({ query: payload })` pushed the `ServiceCall` wrapper, producing `?action=...&payload=[object Object]` | `AppSearchup.vue` |
| `errorMessage` branch unreachable — `credentials.length === 0` is checked first and always matches on failure | `AppSearchup.vue` |
| `submittedServiceName` written but never read; the template passes the live `data.ServiceName`, so sort shifts while typing | `AppSearchup.vue` |
| `exportCSVDecider` called with 5 arguments after being refactored to take 1 | `PaginationGrid.vue` |
| `ref(pageNumber)` returns the *same* ref, not a copy — `ref()` passes a ref through unchanged, so the page input wrote straight to the model and the typing buffer never existed | `PaginationGridPageTracker.vue` |
| A page assigned onto a plain `{...grid}` object: not reactive, and the prop reference never changed, so the display never re-rendered | `PaginationGrid.vue` |
| `withinCategorySortOption` passed to `sortDecider` as a hardcoded `null`, so choosing a within-category sort did nothing | `PaginationGrid.vue` |
| `watch(() => grid.rows, …)` registered for only two of the four locations, so `searchup` never reacted to replaced rows | `PaginationGrid.vue` |
| `allData[pageNumber - 1]` duplicated in six places, each able to yield `undefined` on an empty page set | `PaginationGrid.vue`, `PaginationGridPageTracker.vue` |
| A derived page kept in a `ref` and synced by `watch(pageNumber, …)`: silent on mount and on sort changes, since `pageNumber` itself did not change | `PaginationGrid.vue` |

---

### 6.12 Narrowing is per-branch, and never travels between values

`GridData` is a discriminated union keyed on `location` ([gridType.ts](../PlatinumCredentialManager.Client/src/types/gridType.ts)). `PaginationGrid.vue` has to hand the display component one *page* of rows, still shaped as a `GridData`. The obvious split — one computed for the page, a second to attach it — cannot be typed:

```ts
const displayedRows = computed(() => pages.value[pageNumber.value - 1]);

const modifiedGrid = computed(() => {
    if (grid.location === 'allCategories') {
        return { ...grid, rows: displayedRows.value };   // TS2322
    }
    ...
});
```

Checking `grid.location` narrows `grid`. It says nothing about `displayedRows`, which was computed elsewhere and stays `CategorySummary[] | CredentialSummary[] | CredentialSearchResult[]`. So the object pairs a known `location` with an unknown `rows`, and nothing rules out `{ location: 'allCategories', rows: CredentialSearchResult[] }` — not a member of the union.

Two rules are behind this:

- **Narrowing applies to the value you checked, and only inside that block.** It is not a fact recorded about the function.
- **TypeScript never correlates two separate values.** Knowing something about `grid` implies nothing about a different variable, prop or ref — even when the two always change together at runtime.

The fix is to derive the rows *from the narrowed value, in the same block*:

```ts
const modifiedGrid = computed(() => {
    if (grid.location === 'credential') return grid;          // not paginated
    if (grid.location === 'allCategories') {
        // grid.rows is CategorySummary[] here, so the page is too
        const page = paginateData(sortDecider(sortOption.value, withinCategorySortOption.value, grid.rows))[pageNumber.value - 1];
        return { ...grid, rows: page };
    }
    // one branch per remaining location
});
```

Generic helpers are fine: `sortDecider<T extends DataRow>` and `paginateData<T>` pass the concrete type straight through. What must be repeated per branch is the **call** — its argument has to be the narrowed `grid.rows` — and the **spread**, where the location and the rows are paired. Three near-identical branches is the right amount of duplication here; it is what buys real narrowing in `PaginationGridDataDisplay.vue`, where `category.id` becomes `number` instead of `number | undefined`.

The same rule explains three other errors in that folder:

| Where | The pair TypeScript will not correlate |
|---|---|
| `exportCSVDecider(location, rows, …)` in the template | `location === 'searchup'` does not narrow `grid`, so `grid.params` is `TS2339` |
| `redirectCredentialQuery` | reads `grid.params` with no `location` check at all |
| `currentData: DataRow[]` as a second prop | `grid.location` cannot narrow a separately declared prop |

That last row is why `DataRow` existed: with every field optional it is the lowest common denominator that compiles in all four branches, at the cost of `number | undefined` everywhere it is read. Carrying the discriminant *with* the rows is what replaces it.

**Template expressions never narrow.** `:grid="{ ...grid, rows: page }"` is checked against the whole union — there is no `v-if` scope that narrows a sibling attribute, so this can never type-check. Build the object in `<script setup>` and bind the finished result.

---

### 6.13 Workflow

- `npm run type-check` reads files **on disk**; IDE squiggles come from Volar's in-memory copy. When they disagree, the script is the authority.
- An error's reported line is often not where the fix goes. Template errors point at the binding; the cause is usually a declaration elsewhere.
- Fix `TS2307` first, then work **leaves-up**: a child's `defineProps` is what the parent's bindings are checked against, so typing parents first means doing the work twice.
- Expect counts to rise before they fall. A vague error becoming a specific one is progress.

---

## 7. Quick reference

| Syntax | Meaning | Example in this codebase |
|---|---|---|
| `A \| B` | Either A or B | `number \| undefined` |
| `"a" \| "b"` | Only these exact strings | `FormAction` |
| `T[]` | Array of T | `string[]` |
| `type X<T>` | Generic type | `HandlerResult<T>` |
| `Record<K, V>` | Object used as a map | `Record<string, number>` |
| `keyof T` | Union of T's property names | `keyof ImportProblem` |
| `T["k"]` | Type of property k | `ErrorData["extensions"]` |
| `Extract<U, X>` | Keep union members in X | `ImportAction` |
| `Partial<T>` | All properties optional | `ErrorData.extensions` |
| `as const` | Keep literals, make readonly | `HEADER_ERROR_NAMES` |
| `readonly T[]` | Array you can't modify | `satisfies readonly (...)[]` |
| `satisfies T` | Check against T, keep the precise type | `ERROR_NAMES` arrays |
| `typeof X[number]` | Union of an array's elements | `HeaderErrorName` |
| `a ?? b` | `b` if `a` is null/undefined | `response[name] ?? 0` |
| `if (x.valid)` | Narrow a discriminated union | `HandlerResult` |
| `x is T` | Custom narrowing function | `isImportAction` |
