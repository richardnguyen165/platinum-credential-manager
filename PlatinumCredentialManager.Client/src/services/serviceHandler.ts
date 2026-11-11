import { deleteCategory, exportCategories, getCategory, importCategories, postCategory, putCategory } from "@/services/categoryService.js";
import { deleteCredential, getAllCredentials, getCredential, importCredentials, postCredential, putCredential, searchCredential } from "@/services/credentialService.js";
import { getUser, postNewUser } from "@/services/userService.js";
import type { backendResponse } from "./serviceType.js";
import type { ServiceCall } from  "./requestType.js";



const ERROR_NAMES: string[] = [
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

export async function serviceHandler(serviceCall: ServiceCall): (Promise<backendResponse>) {
    let response: backendResponse | undefined;

    try{
        switch(serviceCall.action){
            case "get-user":
                response = await getUser();
                break;
            case "post-new-user":
                response = await postNewUser();
                break;
            case "allCategories":
                response = await importCategories(serviceCall.payload);
                break;
            case "create-category":
                response = await postCategory(serviceCall.payload);
                break;
            case "get-category":
                response = await getCategory(serviceCall.payload["id"]);
                break;
            case "edit-category":
                response = await putCategory(serviceCall.payload["id"], serviceCall.payload["data"]);
                break;
            case "delete-category":
                response = await deleteCategory(serviceCall.payload["id"]);
                break;
            case "export-categories":
                response = await exportCategories();
                break;
            case "getAllCredentials":
                response = await getAllCredentials();
                break;
            case "search-credential":
                response = await searchCredential(serviceCall.payload);
                break;
            case "get-credential":
                response = await getCredential(serviceCall.payload["id"]);
                break;
            case "edit-credential":
                response = await putCredential(serviceCall.payload["id"], serviceCall.payload["data"]);
                break;
            case "delete-credential":
                response = await deleteCredential(serviceCall.payload["id"]);
                break;
            case "allCredentials":
                response = await importCredentials(serviceCall.payload["data"], serviceCall.payload["id"]);
                break;
            case "create-credential":
                response = await postCredential(serviceCall.payload);
                break;
        }
    } catch (err) {
        console.log(err);
        return {
            "valid": false,
            "data": null
        }
    }

    if (response?.valid == undefined){
        const currentErrorObject: backendResponse = {
            "valid": false,
            "data": {
                status: response?.data.status,
                title: response?.data.title,
                detail: response?.data.detail,
                extensions: {}
            }
        };

        for (const name of ERROR_NAMES){
            if (name in response?.data) currentErrorObject["data"]["extensions"][name] = response?.data[name];
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