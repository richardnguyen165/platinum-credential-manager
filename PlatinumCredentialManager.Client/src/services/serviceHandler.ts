import { deleteCategory, exportCategories, getCategory, importCategories, postCategory, putCategory } from "@/services/categoryService.js";
import { deleteCredential, getAllCredentials, getCredential, importCredentials, postCredential, putCredential, searchCredential } from "@/services/credentialService.js";
import { getUser, postNewUser } from "@/services/userService.js";
import type { HandlerResult, ErrorData, ServiceResult, ResponseFor } from "@/types/serviceType.js";
import type { ServiceCall } from  "@/types/requestType.js";
import type { ImportProblem } from "@/types/responseType.js";

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
] as const satisfies readonly (keyof ImportProblem)[];
// as const, keeps the exact names in the array's type
// satsifies checks if every name is a real field of ImportProblem

// Defines what HandlerResult returns specifically (overload signature, what the function looks from the outside)
export function serviceHandler<C extends ServiceCall>(serviceCall: C): Promise<HandlerResult<ResponseFor[C["action"]]>>;
export async function serviceHandler(serviceCall: ServiceCall): Promise<HandlerResult<unknown>>{
    let response: ServiceResult<unknown> | undefined;

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

    if (!response) return { valid: false, data: null }

    if (!response.valid){

        const problem = response.data;
        const currentErrorObject: ErrorData = {
            status: problem?.status,
            title: problem?.title,
            detail: problem?.detail,
            extensions: {}
        };

        if (problem){
            const raw = problem as Partial<ImportProblem>; // Property in ImportProblem, property could become optional 
            for (const name of ERROR_NAMES){
                const count = raw[name];
                if (count !== undefined) currentErrorObject.extensions[name] = count; // meaning that problem does not exist
            }
        }
        
        return { valid: false, data: currentErrorObject };
    }
    else{
        return {
            "valid": true,
            "data": response["data"]
        };
    }
}