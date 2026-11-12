import type { ProblemDetails, ValidationProblemDetails } from "./responseType.js"

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