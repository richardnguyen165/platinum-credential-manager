import type { CurrentEntries } from "./formType.js";
import type { SearchQuery } from "./requestType.js";
import type { CategorySummary, CredentialSearchResult, CredentialSummary } from "./responseType.js";

export type GridLocation = 'allCategories' | 'allCredentials' | 'credential' | 'searchup';

export type SearchParams = {
  CategoryName?: string,
  ServiceName?: string,
  UserDateChoice: string,
  CreateUpdateChoice: string,
  StartDate?: string,
  EndDate?: string,
} 

export type GridData = 
{
  location: 'allCategories',
  rows: CategorySummary[]
}
| 
{
  location: 'allCredentials',
  rows: CredentialSummary[]
}
| 
{
  location: 'searchup',
  rows: CredentialSearchResult[],
  params: Partial<SearchQuery>,
  credentialMatch: string
}
| 
{
  location: 'credential',
  rows: CurrentEntries
}