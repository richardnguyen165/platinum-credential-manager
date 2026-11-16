import type { SearchParams } from "@/types/gridType.js";
import type { CredentialSearchResult } from "@/types/responseType.js";

export function parsePayload(data: SearchParams | CredentialSearchResult[]){
    const polishedData: any = {}

    for (const [key, value] of Object.entries(data)) {
        // Preserving 0
        let preserveZero = value ?? false;
        if (preserveZero || value) polishedData[key] = value;
    }

    return polishedData;
}