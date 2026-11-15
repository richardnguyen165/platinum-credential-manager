export type sortAction = 'azAscendingService' | 'azDescendingService' | 'azAscendingCategory' | 'azDescendingCategory' | 'dateCreatedAscending' | 'dateCreatedDescending' | 'dateUpdatedAscending' | 'dateUpdatedDescending' | 'searchup'

export interface DataRow {
    serviceName?: string;
    categoryName?: string;
    dateCreated?: string;
    dateLastUpdated?: string;
}
