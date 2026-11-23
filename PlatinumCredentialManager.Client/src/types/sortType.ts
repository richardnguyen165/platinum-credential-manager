export type SortAction = '' | 'azAscendingService' | 'azDescendingService' | 'azAscendingCategory' | 'azDescendingCategory' | 'dateCreatedAscending' | 'dateCreatedDescending' | 'dateUpdatedAscending' | 'dateUpdatedDescending' | 'searchup'

export interface DataRow {
    id?: number;
    serviceName?: string;
    categoryName?: string;
    dateCreated?: string;
    dateLastUpdated?: string;
    username?: string;
    password?: string;
}
