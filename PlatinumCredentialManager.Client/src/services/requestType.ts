export interface CredentialForm {
    ServiceName: string,
    Username: string,
    Password: string,
    categoryId: number
}

export interface CategoryForm {
    CategoryName: string,
    categoryId?: number
}

export interface SearchQuery {
    CreateUpdateChoice: "Create" | "Update",
    UserDateChoice: "lastSevenDays" | "lastThirtyDays" | "lastYear" | "boundedByDates",
    CategoryName?: string,
    ServiceName?: string,
    StartDate?: string,
    EndDate?: string
}

export type ImportCategories = File;

export type importCredentials = File;