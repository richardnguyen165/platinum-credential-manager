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

type Id = string | number;
type WithId = { id: Id };
type WithIdAnd<T> = { id: Id; data: T };

export type ServiceCall = 
    | { action: "get-user" }
    | { action: "post-new-user" }
    | { action: "export-categories" }
    | { action: "getAllCredentials" }
    | { action: "allCategories";      
        payload: File }
    | { action: "create-category";    
        payload: CategoryForm }
    | { action: "search-credential";  
        payload: SearchQuery }
    | { action: "create-credential";  
        payload: CredentialForm }
    | { action: "get-category";       
        payload: WithId }
    | { action: "delete-category";    
        payload: WithId }
    | { action: "get-credential";     
        payload: WithId }
    | { action: "delete-credential";  
        payload: WithId }
    | { action: "edit-category";      payload: WithIdAnd<CategoryForm> }
    | { action: "edit-credential";    payload: WithIdAnd<CredentialForm> }
    | { action: "allCredentials";     payload: WithIdAnd<File> };

export type ImportCategories = File;

export type ImportCredentials = File;