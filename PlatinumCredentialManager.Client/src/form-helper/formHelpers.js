export function titleHelper(action) {
  switch (action) {
    case "create-category":
      return "Create A Category";
    case "edit-category":
      return "Edit A Category";
    case "delete-category":
      return "Delete A Category";
    case "create-credential":
      return "Create A Credential";
    case "edit-credential":
      return "Edit A Credential";
    case "delete-credential":
      return "Delete A Credential";
    case "allCategories":
      return "Import Credentials For Any Category"
    case "allCredentials":
      return "Import Credentials Into A Category"
    default:
      return "";
  }
}

export function ruleHelper(action) {
  switch(action) {
    case "allCategories":
      return [
        { id: 0, text: "Please add the following headers to the first row of the CSV: Category Name, Service Name, Username and Password"},
        { id: 1, text: "Usernames can be blanked out."},
        { id: 2, text: "Serivce names that share the same name as with another credential in a category will be rejected."},
        { id: 3, text: "Service names that are left empty means that credential/row will be rejected."},
        { id: 4, text: "Categories that are left blanked out means that credential/row will be rejected."},
        { id: 5, text: "Credential that have a category sharing the same name as another category means that credential will be added into that category."},
        { id: 6, text: "Passwords that are left empty means that credential/row will be rejected."},
        { id: 7, text: "Credentials that share a same service name with another credential will be ignored."},
        { id: 8, text: "Any rejection means the CSV file in full will be rejected, and a corresponding error count will be returned to the user highlighting errors."}
      ]
    case "allCredentials":
      return [
        { id: 0, text: "Please add the following headers to the first row of the CSV: Service Name, Username and Password"},
        { id: 1, text: "Usernames can be blanked out."},
        { id: 2, text: "Serivce names that share the same name as with another credential in a category will be rejected."},
        { id: 3, text: "Service names that are left empty means that credential/row will be rejected."},
        { id: 4, text: "Credentials that share the same name as with another credential will be rejected."},
        { id: 5, text: "Passwords that are left empty means that credential/row will be rejected."},
        { id: 6, text: "Duplicate credentials will be ignored."},
        { id: 7, text: "Any rejection means the CSV file in full will be rejected, and a corresponding error list will be returned to the user highlighting errors."}
      ]
    default:
      return [];
  }
}

export function descriptionHelper(action) {
  switch (action) {
    case "create-category":
      return "Please create a category by adding a name.";
    case "edit-category":
    case "edit-credential":
      return "Please note that all changes to the input fields will be saved when the OK button is pressed.";
    case "delete-category":
      return "Are you sure you want to delete this category?";
    case "create-credential":
      return "Please create a credential by filling out the input fields below.";
    case "delete-credential":
      return "Are you sure you want to delete this credential?";
    case "allCategories":
    case "allCredentials":
      return "Please follow the CSV format provided in the image below."
    default:
      return "";
  }
}

export function inputHelper(action, allCategories, currentEntries, categoryId) {
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

  let categoryOptions = {
      key: "categoryId",
      label: "Category",
      type: "select",
      value: categoryId
  };

  if (action === "edit-credential"){
    const allOptions = [];

    for (const categorySet of allCategories){
      allOptions.push({
        label: categorySet["categoryName"],
        id: categorySet["id"],
        selected: categoryId === categorySet["id"]
      })
    }

    categoryOptions["options"] = allOptions;
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