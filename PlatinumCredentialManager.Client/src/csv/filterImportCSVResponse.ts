import type { ImportProblem } from "@/types/responseType.js";
import type { ErrorData } from "@/types/serviceType.js";
import type { FormAction } from "@/types/formType.js";

const HEADER_ERROR_NAMES = [
    "missingColumnError",
    "tooManyColumnError",
    "wrongColumnName"
] as const satisfies readonly (keyof ImportProblem)[];

const CREDENTIAL_ERROR_NAMES = [
    "missingRowEntryError",
    "tooManyRowEntriesError",
    "noServiceName",
    "duplicateServiceName",
    "serviceNameTooLong",
    "usernameTooLong",
    "noPassword",
    "passwordTooLong",
    "noCredentials"
] as const satisfies readonly (keyof ImportProblem)[];

const CATEGORY_ERROR_NAMES = [
    ...CREDENTIAL_ERROR_NAMES,
    "noCategoryName",
    "categoryNameTooLong"
] as const satisfies readonly (keyof ImportProblem)[];

type ImportExtensions = ErrorData["extensions"];
type ImportAction = Extract<FormAction, "allCategories" | "allCredentials">;
type HeaderErrorName = typeof HEADER_ERROR_NAMES[number];
type RowErrorName = typeof CATEGORY_ERROR_NAMES[number];

export type ImportCSVViolation = {
    type: "file" | "header" | "credentials",
    errors: string[]
};

// Extensions are Partial, so a missing key counts as "no error"
function errorCount(response: ImportExtensions, name: keyof ImportProblem): number {
    return response[name] ?? 0;
}

function rowErrorHelper(rowError: RowErrorName, response: ImportExtensions): string {
    const count = errorCount(response, rowError);
    const plural = count > 1 ? 's' : '';

    if (rowError === "missingRowEntryError"){
        return `${count} credential${plural} have less than the required number of columns.`
    }
    else if (rowError === "tooManyRowEntriesError"){
        return `${count} credential${plural} have more than the required number of columns.`
    }
    else if (rowError === "noServiceName"){
        return `${count} credential${plural} have no service names.`
    }
    else if (rowError === "duplicateServiceName"){
        return `${count} credential${plural} have duplicate service names.`
    }
    else if (rowError === "serviceNameTooLong"){
        return `${count} credential${plural} have service names that exceed 100 characters.`
    }
    else if (rowError === "usernameTooLong"){
        return `${count} credential${plural} have user names that exceed 100 characters.`
    }
    else if (rowError === "noPassword"){
        return `${count} credential${plural} have no passwords.`
    }
    else if (rowError === "passwordTooLong"){
        return `${count} credential${plural} have passwords that exceed 100 characters.`
    }
    else if (rowError === "noCategoryName"){
        return `${count} credential${plural} have no correlating category names.`
    }
    else if (rowError === "noCredentials"){
        return `There are no credentials to process.`
    }
    else {
        return `${count} credential${plural} have category names that exceed 100 characters.`
    }
}


function headerErrorHelper(headerError: HeaderErrorName, response: ImportExtensions): string {
    const count = errorCount(response, headerError);
    const plural = count > 1 ? 's' : '';

    if (headerError === "missingColumnError"){
        return `${count} columns are missing from the header.`
    }
    else if (headerError === "tooManyColumnError"){
        return `Header has ${count} extra column${plural}.`
    }
    else {
        return `Header has ${count} wrong column${plural}. Please follow the format.`
    }
}

export function filterImportCSVResponse(action: ImportAction, response: ImportExtensions): ImportCSVViolation | undefined {
    // Step 1: Check for an empty file
    if (errorCount(response, "emptyFile") !== 0){
        return {
            type: "file",
            errors: ["Please submit a non-empty file!"]
        }
    }

    const headerErrors: string[] = [];
    // Step 2: Check the column headers
    for (const error of HEADER_ERROR_NAMES){
        if (errorCount(response, error) !== 0){
            headerErrors.push(headerErrorHelper(error, response))
        }
    }
    if (headerErrors.length > 0) return {
        type: "header",
        errors: headerErrors
    };

    // Step 3: Check the rows
    const rowErrors: string[] = [];
    const rowErrorNames = action === "allCategories" ? CATEGORY_ERROR_NAMES : CREDENTIAL_ERROR_NAMES;
    for (const error of rowErrorNames){
        if (errorCount(response, error) !== 0){
            rowErrors.push(rowErrorHelper(error, response))
        }
    }
    if (rowErrors.length > 0) {
        return {
            type: "credentials",
            errors: rowErrors
        }
    }

    return undefined;
}
