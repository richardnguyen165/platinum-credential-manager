// API RESPONSE VALUES - 200 VALUES

export interface CategorySummary {
    id: number,
    categoryName: string
}

export interface CategoryListItem {
    id: number,
    categoryName: string
}

export interface CredentialSummary {
    id: number,
    serviceName: string,
    username: string,
    dateCreated: string,
    dateLastUpdated: string
}

export interface CategorySummary {
    id: number,
    serviceName: string,
    username: string,
    dateCreated: string,
    dateLastUpdated: string
}

export interface CategoryDetail {
    id: number, 
    categoryName: string,
    credentials: CredentialSummary[]
}

export interface ExportCredential {
  id: number,
  serviceName: string,
  username: string,
  password: string,
  dateCreated: string,
  dateLastUpdated: string
}

export interface ExportCategory {
    id: number,
    categoryName: string,
    credentials: ExportCredential[]
}

export interface CredentialDetail {
    id: number,
    serviceName: string,
    username: string,
    password: string,
    dateCreated: string,
    dateLastUpdated: string,
    categoryName: string,
    categoryId: number
}

export interface CredentialSearchResult {
    credentialId: number,
    categoryId: number,
    categoryName: string,
    serviceName: string,
    dateCreated: string,
    dateLastUpdated: string
}

export interface UserProfile {
    id: number,
    keycloakId: string,
    categories: CategorySummary[]
}

export interface ImportProblem {
    type: string,
    title: string,
    status: number,
    detail: string,
    emptyFile: number,
    noCredentials: number,
    missingColumnError: number,
    tooManyColumnError: number,
    wrongColumnName: number,
    missingRowEntryError: number,
    tooManyRowEntriesError: number,
    noCategoryName: number,
    categoryNameTooLong: number,
    noServiceName: number,
    duplicateServiceName: number,
    serviceNameTooLong: number,
    usernameTooLong: number,
    noPassword: number,
    passwordTooLong: number
}

// API RESPONSE VALUES - ERROR VALUES

export interface ProblemDetails {
    type: string,
    title: string,
    status: number,
    detail: string | null | undefined,
    traceId: string | null | undefined
}

export interface ValidationProblemDetails extends ProblemDetails {
    errors: {
        [fieldName: string]: string[]
    }
}