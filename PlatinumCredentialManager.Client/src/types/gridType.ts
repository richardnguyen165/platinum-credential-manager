import type { CategorySummary, CredentialDetail, CredentialSearchResult } from "./responseType.js";
import type { SortAction } from "./sortType.js";

export type GridLocation = 'allCategories' | 'allCredentials' | 'credential' | 'searchup';

export type SearchParams = {
  CategoryName?: string,
  ServiceName?: string,
  UserDateChoice: string,
  CreateUpdateChoice: string,
  StartDate?: string,
  EndDate?: string,
} 

export type GridPayload = 
{
  action: 'allCategories',
  data: CategorySummary[]
  sortOption: SortAction
  withinCategorySortOption: string | null
}
|
{
  action: 'allCredentials',
  data: CredentialDetail[]
}
| 
{
  action: 'credential',
  data: CredentialDetail
}
| 
{
  action: 'searchup'
  data: CredentialSearchResult[],
  params: SearchParams
}