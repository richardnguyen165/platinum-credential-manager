export type FormAction =
  | "create-category" | "edit-category" | "delete-category"
  | "create-credential" | "edit-credential" | "delete-credential"
  | "allCategories" | "allCredentials";

export type Rule = {
  id: number,
  text: string
}

export type SelectOption = {
  label: string,
  id: number,
  selected: boolean
}

export type Input = {
  label?: string,
  key?: string,
  id?: string,
  type?: "password" | "select" | "text",
  required?: boolean,
  value?: string | number,
  selected?: string
  options?: SelectOption[]
}

export type CurrentEntries = Partial<{
  CategoryName: string; ServiceName: string; Username: string;
  Password: string; DateCreated: string; DateLastUpdated: string;
}>;
