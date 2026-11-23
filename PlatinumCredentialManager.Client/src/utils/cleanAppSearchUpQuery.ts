import type { LocationQueryValue } from "vue-router";

// Takes one value out of the URL's query string and gives back a string you can trust.
export function cleanAppSearchUpQuery<T extends string>(
    // The URL can hand you one value, several values, or nothing - all three are possible.
    query: LocationQueryValue | LocationQueryValue[] | undefined,
    // What to use instead when the URL value is missing or not allowed.
    alternative: T,
    // Optional list of the only values you accept, for the dropdown fields.
    constants?: readonly T[]
): T {
    // "?x=a&x=b" repeats the key and gives an array, so take the first value.
    const element = Array.isArray(query) ? query[0] : query;
    // A loose == catches null AND undefined, so anything missing falls back here.
    if (element == undefined) return alternative;
    // If a list was given, fall back unless the value is in it (the cast just lets .includes compare plain strings).
    if (constants && !(constants as readonly string[]).includes(element)) return alternative;
    // The checks above proved this is a real, allowed value, so label it T and return it.
    return element as T;
}
