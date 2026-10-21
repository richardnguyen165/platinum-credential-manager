const HEADER_ERROR_NAMES = [
    "missingColumnError",
    "tooManyColumnError",
    "wrongColumnName"
]

const CREDENTIAL_ERROR_NAMES = [
    "missingRowEntryError",
    "tooManyRowEntriesError",
    "noServiceName",
    "duplicateServiceName",
    "serviceNameTooLong",
    "usernameTooLong",
    "noPassword",
    "passwordTooLong",
    "noCredentials"
]

const CATEGORY_ERROR_NAMES = [
    ...CREDENTIAL_ERROR_NAMES,
    "noCategoryName",
    "categoryNameTooLong"
]

function rowErrorHelper(rowError, response){
    if (rowError === "missingRowEntryError"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have less than the required number of columns.`
    }
    else if (rowError === "tooManyRowEntriesError"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have more than the required number of columns.` 
    }
    else if (rowError === "noServiceName"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have no service names.`
    }
    else if (rowError === "duplicateServiceName"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have duplicate service names.`
    }
    else if (rowError === "serviceNameTooLong"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have service names that exceed 100 characters.`
    }
    else if (rowError === "usernameTooLong"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have user names that exceed 100 characters.`
    }
    else if (rowError === "noPassword"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have no passwords.`
    }
    else if (rowError === "passwordTooLong"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have passwords that exceed 100 characters.`
    }
    else if (rowError === "noCategoryName"){
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have no correlating category names.`
    }
    else if (rowError === "noCredentials"){
        return `There are no credenitals to process.`
    }
    else {
        return `${response[rowError]} credential${response[rowError] >  1 ?  's' : ''} have category names that exceed 100 characters.`
    }
}


function headerErrorHelper(headerError, response) {
    if (headerError === "missingColumnError")
        return `${response[headerError]} columns are missing from the header.`
    else if (headerError === "tooManyColumnError"){
        return `Header has ${response[headerError]} extra column${'s' ? response[headerError] >  1 : ''}.`
    } 
    else {
        return `Header has ${response[headerError]} wrong column${'s' ? response[headerError] >  1 : ''}. Please follow the format.`
    }
}

export function filterImportCSVResponse(action, response){
    // Step 1: Check for an empty file
    if (response["emptyFile"] !== 0){
        return {
            "type": "file",
            "errors": ["Please submit a non-empty file!"]
        }
    }

    const headerErrors = [];
    // Step 2: Check the column headers
    for (const error of HEADER_ERROR_NAMES){
        if (response[error] !== 0){
            headerErrors.push(headerErrorHelper(error, response))
        }
    }
    if (headerErrors.length > 0) return {
        "type": "header",
        "errors": headerErrors
    };

    // Step 3: Check the rows
    const rowErrors = [];
    if (action === "allCategories"){
        for (const error of CATEGORY_ERROR_NAMES){
            if (response[error] !== 0){
                rowErrors.push(rowErrorHelper(error, response))
            }
        }
    } else {
        for (const error of CREDENTIAL_ERROR_NAMES){
            if (response[error] !== 0){
                rowErrors.push(rowErrorHelper(error, response))
            }
        }
    }
    if (rowErrors.length > 0) {
        return {
            "type": "credentials",
            "errors": rowErrors
        }
    }
}