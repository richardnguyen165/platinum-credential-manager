import type { CategoryDetail, CredentialDetail, CredentialSearchResult, ProblemDetails, UserProfile, ValidationProblemDetails } from "./responseType.js"

export type ErrorData = {
    status?: number | undefined,
    title?: string | undefined,
    detail?: string | null | undefined,
    extensions: Partial<Record<string, number>>
}

export type ServiceResult<T> = 
    | { valid: true, data: T }
    | { valid: false, data: ProblemDetails | ValidationProblemDetails | undefined }

export type HandlerResult<T = unknown> = 
    | { valid: true, data: T }
    | { valid: false, data: ErrorData | null }

export type ResponseFor = {
    "get-user": UserProfile,
    "post-new-user": UserProfile,
    "allCategories": string,
    "create-category": CategoryDetail,
    "get-category": CategoryDetail,
    "edit-category": void,
    "delete-category": void,
    "export-categories": CategoryDetail,
    "getAllCredentials": CredentialDetail[],
    "search-credential": CredentialSearchResult[],
    "get-credential": CredentialDetail,
    "edit-credential": void,
    "delete-credential": void,
    "allCredentials": string,
    "create-credential": CredentialDetail
};
