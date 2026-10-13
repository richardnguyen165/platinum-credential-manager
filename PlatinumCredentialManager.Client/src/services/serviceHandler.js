import { keycloak } from "@/config/keycloak";
import { deleteCategory, exportCategories, getCategory } from "./categoryService";
import { getAllCredentials, getCredential, importCredentials, postCredential, putCredential, searchCredential } from "./credentialService";
import { getUser, postNewUser } from "./userService";


export async function serviceHandler(action) {
    let response;

    switch(action){
        case "get-user":
            response = await getUser();
            break;
        case "post-new-user":
            response = await postNewUser();
            break;
        case "get-category":
            response = await getCategory();
            break;
        case "export-categories":
            response = await exportCategories();
            break;
        case "getAllCredentials":
            response = await getAllCredentials();
            break;
        case "search-credential":
            response = await searchCredential();
            break;
        case "get-credential":
            response = await getCredential();
            break;
        case "delete-category":
            response = await deleteCategory();
            break;
        case "import-credentials":
            response = await importCredentials();
            break;
        case "post-credential":
            response = await postCredential();
            break;
        case "put-credential":
            response = await putCredential();
            break;
    }

    if (!response["valid"]){
        if(response.data.status == 401){
            console.log("Authentication expired - logging out..")
            keycloak.logout();
        }
        return {
            "valid": false,
            "data": {
                status: response.data.status,
                title: response.data.title,
                detail: response.data.detail,
                extensions: response?.extensions
            }
        }
    }
    else{
        return {
            "valid": true,
            "data": response["data"]
        };
    }
}