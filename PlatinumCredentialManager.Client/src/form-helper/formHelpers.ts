import type { CategoryListItem } from "@/services/responseType.js";

type FormAction =
  | "create-category" | "edit-category" | "delete-category"
  | "create-credential" | "edit-credential" | "delete-credential"
  | "allCategories" | "allCredentials";

type Rule = {
  id: number,
  text: string
}

type SelectOption = {
  label: string,
  id: number,
  selected: boolean
}

type Input = {
  label?: string,
  key?: string,
  id?: string,
  type?: "password" | "select" | "text",
  required?: boolean,
  value?: string | number,
  selected?: string
  options?: SelectOption[]
}

type CurrentEntries = Partial<{
  CategoryName: string; ServiceName: string; Username: string;
  Password: string; DateCreated: string; DateLastUpdated: string;
}>;

const TITLES: Record<FormAction, string> = {
  'create-category': 'Create A Category',
  'edit-category': 'Edit A Category',
  'delete-category': 'Delete A Category',
  'create-credential': 'Create A Credential',
  'edit-credential': 'Edit A Credential',
  'delete-credential': 'Delete A Credential',
  'allCategories': 'Import Credentials For Any Category',
  'allCredentials': ''
}

const DESCRIPTIONS: Record<FormAction, string> = {
  'create-category': 'Please create a category by adding a name.',
  'edit-category': 'Please note that all changes to the input fields will be saved when the OK button is pressed.',
  'delete-category': 'Are you sure you want to delete this category?',
  'create-credential': 'Please create a credential by filling out the input fields below.',
  'edit-credential': 'Please note that all changes to the input fields will be saved when the OK button is pressed.',
  'delete-credential': 'Are you sure you want to delete this credential?',
  'allCategories': 'Please follow the CSV format provided in the image below',
  'allCredentials': 'Please follow the CSV format provided in the image below'
}

// Partial is needed because not all FormAction is covered
const RULES: Partial<Record<FormAction, Rule[]>> = {
  'allCategories': [
    { id: 0, text: "Please add the following headers to the first row of the CSV: Category Name, Service Name, Username and Password" },
    { id: 1, text: "Usernames can be blanked out." },
    { id: 2, text: "Service names that share the same name as with another credential in a category will be rejected." },
    { id: 3, text: "Service names that are left empty means that credential/row will be rejected." },
    { id: 4, text: "Categories that are left blanked out means that credential/row will be rejected." },
    { id: 5, text: "Credential that have a category sharing the same name as another category means that credential will be added into that category." },
    { id: 6, text: "Passwords that are left empty means that credential/row will be rejected." },
    { id: 7, text: "Credentials that share a same service name with another credential will be ignored." },
    { id: 8, text: "Any rejection means the CSV file in full will be rejected, and a corresponding error count will be returned to the user highlighting errors." }
  ],
  'allCredentials': [
    { id: 0, text: "Please add the following headers to the first row of the CSV: Service Name, Username and Password" },
    { id: 1, text: "Usernames can be blanked out." },
    { id: 2, text: "Service names that share the same name as with another credential in a category will be rejected." },
    { id: 3, text: "Service names that are left empty means that credential/row will be rejected." },
    { id: 4, text: "Credentials that share the same name as with another credential will be rejected." },
    { id: 5, text: "Passwords that are left empty means that credential/row will be rejected." },
    { id: 6, text: "Duplicate credentials will be ignored." },
    { id: 7, text: "Any rejection means the CSV file in full will be rejected, and a corresponding error list will be returned to the user highlighting errors." }
  ]
}

export function titleHelper(action: FormAction): string {
  return TITLES[action] ?? "";
}

export function descriptionHelper(action: FormAction): string {
  return DESCRIPTIONS[action] ?? "";
}

export function ruleHelper(action: FormAction): Rule[] {
  return RULES[action] ?? [];
}

export function inputHelper(action: FormAction, allCategories: CategoryListItem[], currentEntries: CurrentEntries, categoryId: number | string) {
  const credentialArray = [
        {
          key: "ServiceName",
          label: "Service Name",
          type: "text",
          required: true,
          value: currentEntries?.ServiceName,
        },
        {
          key: "Username",
          label: "Username",
          type: "text",
          required: true,
          value: currentEntries?.Username,
        },
        {
          key: "Password",
          label: "Password",
          type: "password",
          required: false,
          value: currentEntries?.Password,
        },
  ];

  let categoryOptions: Input = {
      key: "categoryId",
      label: "Category",
      type: "select",
      value: categoryId
  };

  if (action === "edit-credential"){
    categoryOptions.options = allCategories.map((category: CategoryListItem): SelectOption => ({
      label: category.categoryName,
      id: category.id,
      selected: categoryId === category.id
    }));
  }
    
  switch (action) {
    case "create-category":
    case "edit-category":
      return [
        {
          key: "CategoryName",
          label: "Category Name",
          type: "text",
          required: true,
          value: currentEntries?.CategoryName,
        },
      ];
    case "edit-credential":
      return [categoryOptions, ...credentialArray];
    case "create-credential":
      return credentialArray;
    case "allCredentials":
    case "allCategories":
      return [
        {
          key: "importCSV",
          label: "Import CSV",
          type: "file",
          required: true,
          value: "",
        }
      ];
    default:
      return [];
  }
}