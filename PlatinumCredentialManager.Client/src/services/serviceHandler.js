import { keycloak } from "@/config/keycloak";
import { deleteCategory, exportCategories, getCategory, importCategories, postCategory, putCategory } from "./categoryService";
import { deleteCredential, getAllCredentials, getCredential, importCredentials, postCredential, putCredential, searchCredential } from "./credentialService";
import { getUser, postNewUser } from "./userService";

const ERROR_NAMES = [
    "emptyFile",
    "missingColumnError",
    "tooManyColumnError",
    "wrongColumnName",
    "missingRowEntryError",
    "tooManyRowEntriesError",
    "noServiceName",
    "duplicateServiceName",
    "serviceNameTooLong",
    "usernameTooLong",
    "noPassword",
    "passwordTooLong",
    "noCategoryName",
    "categoryNameTooLong"
]

export async function serviceHandler(action, payload = null) {
    let response;

    switch(action){
        case "get-user":
            response = await getUser();
            break;
        case "post-new-user":
            response = await postNewUser();
            break;
        case "allCategories":
            response = await importCategories(payload);
            break;
        case "create-category":
            response = await postCategory(payload);
            break;
        case "get-category":
            response = await getCategory(payload["id"]);
            break;
        case "edit-category":
            response = await putCategory(payload["id"], payload["data"]);
            break;
        case "delete-category":
            response = await deleteCategory(payload["id"]);
            break;
        case "export-categories":
            response = await exportCategories();
            break;
        case "getAllCredentials":
            response = await getAllCredentials();
            break;
        case "search-credential":
            response = await searchCredential(payload);
            break;
        case "get-credential":
            response = await getCredential(payload["id"]);
            break;
        case "edit-credential":
            response = await putCredential(payload["id"], payload["data"]);
            break;
        case "delete-credential":
            response = await deleteCredential(payload["id"]);
            break;
        case "allCredentials":
            response = await importCredentials(payload["data"], payload["id"]);
            break;
        case "create-credential":
            response = await postCredential(payload);
            break;
    }

    if (!response["valid"]){
        if(response.data.status == 401){
            console.log("Authentication expired - logging out..")
            keycloak.logout();
        }
        const currentErrorObject = {
            "valid": false,
            "data": {
                status: response.data.status,
                title: response.data.title,
                detail: response.data.detail,
                extensions: {}
            }
        };

        for (const name of ERROR_NAMES){
            if (name in response.data) currentErrorObject["data"]["extensions"][name] = response.data[name];
        }
        
        return currentErrorObject;
    }
    else{
        return {
            "valid": true,
            "data": response["data"]
        };
    }
}