import type { InjectionKey } from "vue";

// Typed keys for provide/inject. Both sides must import the same key,
// a Symbol never matches the old "categoryId" string.
// null = the page has no category/credential (e.g. AppCategories, AppSearchup).
export const categoryIdKey: InjectionKey<number | null> = Symbol("categoryId");
export const credentialIdKey: InjectionKey<number | null> = Symbol("credentialId");
