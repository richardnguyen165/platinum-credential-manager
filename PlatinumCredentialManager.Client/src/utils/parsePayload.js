export function parsePayload(data){
    const polishedData = {}

    for (const [key, value] of Object.entries(data)) {
        // Preserving 0
        let preserveZero = value ?? false;
        if (preserveZero || value) polishedData[key] = value;
    }

    return polishedData;
}