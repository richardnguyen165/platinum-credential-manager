# TypeScript Types: A Guide Through Our Codebase

As of 2026-11-16 · TypeScript 6.0.3 · vue-tsc 3.3

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
| `tsconfig.json` | Configures the checker. We use `strict: true` and `noUncheckedIndexedAccess: true` (both explained below). |

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

## 6. Quick reference

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
