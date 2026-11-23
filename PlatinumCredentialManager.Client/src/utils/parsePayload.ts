// T is a placeholder for "whatever type the caller passed in"
export function parsePayload<T extends object>(data: T): Partial<T> {
    const polishedData: Partial<T> = {}

    // Object.entries() typed to return [string, any][] => throws aways keys the object actually has => the first item is some string, second item is unknown, so actually get it directly

    // Solution: use the keys
    for (const key of Object.keys(data) as (keyof T)[]) {
        // Preserving 0
        const value = data[key];
        if (value !== null && value !== undefined && value !== "") polishedData[key] = value;
    }

    return polishedData;
}