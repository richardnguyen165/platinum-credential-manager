export type GridLocation = 'allCategories' | 'allCredentials' | 'credential' | 'searchup';

export type SearchParams = {
  CategoryName?: string,
  ServiceName?: string,
  UserDateChoice: string,
  CreateUpdateChoice: string,
  StartDate?: string,
  EndDate?: string,
} 