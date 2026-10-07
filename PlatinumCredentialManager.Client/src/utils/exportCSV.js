import { exportCategories } from "@/services/categoryService";
import { getAllCredentials } from "@/services/credentialService";
import { sortDecider } from "./sort";

export async function exportCSVDecider(action, sortOption = null, data, params = null){
    if (action === 'allCategories') await exportCategoriesAsCSV(sortOption);
    else if (action === 'allCredentials') await exportCredentialsAsCSV(data);
    else if (action === 'credential') await exportCredentialAsCSV(data);
    else await exportSearchCredentialsAsCSV(data, params);
};

// https://stackoverflow.com/questions/69380775/choose-a-folder-where-the-file-will-be-downloadedreactjs
async function getSaveFilePicker(fileName) {
    const opts = {
        suggestedName: fileName,
        types: [
            {
                accept: { "text/plain": [".csv"] },
            },
        ],
    };
    return await window.showSaveFilePicker(opts);
};

// All credentials
async function exportCategoriesAsCSV(sortOption){
    const csvRows = [];

    csvRows.push("All Credentials \n");

    const data = sortDecider(sortOption, await exportCategories());

    // https://www.geeksforgeeks.org/javascript/how-to-create-and-download-csv-file-in-javascript/

    const headers = ["Category Name", "Service Name", "User Name", "Password", "Date Created", "Date Last Updated"];
    csvRows.push(headers.join(","));

    for (let categoryDetail of data){
        for (let credential of categoryDetail.credentials){
            // the quotes inside backticks are needed if the fields themselves have a comma
            const row = [
                `"${categoryDetail.categoryName}"`,
                `"${credential.serviceName ?? ""}"`,
                `"${credential.username ?? ""}"`,
                `"${credential.password ?? ""}"`,
                `"${credential.dateCreated}"`,
                `"${credential.dateLastUpdated}"`,
            ];
            csvRows.push(row.join(","));
        }
    }

    const csvString = csvRows.join('\n');

    const blob = new Blob([csvString], { type: 'text/csv' });

    if (window.hasOwnProperty('showSaveFilePicker')){
        try{
            const fileHandle = await getSaveFilePicker("all_categories.csv");

            // https://developer.mozilla.org/en-US/docs/Web/API/FileSystemFileHandle
            const writable = await fileHandle.createWritable();

            await writable.write(blob);
            await writable.close();
        } catch (error) {
            console.error(error);
        }
    // firefox and safari don't open the window
    } else {
        const url = URL.createObjectURL(blob);
        
        const a = document.createElement('a');
        a.href = url;
        a.download = 'all_categories.csv';
        a.click();
        URL.revokeObjectURL(url); // Clears memory
    }
};

// Per category
async function exportCredentialsAsCSV(credentials) {
    const csvRows = [];

    const allCredentials = await getAllCredentials();

    const data = credentials.map(findPassword);

    function findPassword(credential){
        const firstElement = allCredentials.find(c => c.id === credential.id);
        return {
            ...credential,
            "password": firstElement.password,
            "categoryName": firstElement.categoryName
        }
    }

    console.log(data);
    let categoryName = data[0].categoryName;

    csvRows.push(`All Credentials in ${categoryName} \n`);

    const headers = ["Category Name", "Service Name", "User Name", "Password", "Date Created", "Date Last Updated"];
    csvRows.push(headers.join(","));

    for (let credential of data)
    {
        const credentialRow = [
            `"${categoryName ?? ""}"`,
            `"${credential.serviceName ?? ""}"`,
            `"${credential.username ?? ""}"`,
            `"${credential.password ?? ""}"`,
            `"${credential.dateCreated}"`,
            `"${credential.dateLastUpdated}"`,
        ];
        csvRows.push(credentialRow.join(","));
    }

    const csvString = csvRows.join('\n');

    const blob = new Blob([csvString], { type: 'text/csv' });
    
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = 'all_credentials.csv';
    a.click();
    URL.revokeObjectURL(url);
}

// One credential
async function exportCredentialAsCSV(credential) 
{
    const csvRows = [];

    const headers = ["Category Name", "Service Name", "User Name", "Password", "Date Created", "Date Last Updated"];
    csvRows.push(headers.join(","));

    const credentialData = [
        credential.CategoryName, 
        credential.ServiceName, 
        credential.Username, 
        credential.Password, 
        credential.DateCreated, 
        credential.DateLastUpdated
    ];
    csvRows.push(credentialData.join(","));

    const csvString = csvRows.join('\n');

    const blob = new Blob([csvString], { type: 'text/csv' });

    const defaultFileName = `credential_${credential.ServiceName}_${credential.DateCreated}.csv`;

    if (window.hasOwnProperty('showSaveFilePicker')){
        try{
            const fileHandle = await getSaveFilePicker(defaultFileName);

            // https://developer.mozilla.org/en-US/docs/Web/API/FileSystemFileHandle
            const writable = await fileHandle.createWritable();

            await writable.write(blob);
            await writable.close();
        } catch (error) {
            console.error(error);
        }
    // firefox and safari don't open the window
    } else {
        const url = URL.createObjectURL(blob);
        
        const a = document.createElement('a');
        a.href = url;
        a.download = defaultFileName;
        a.click();
        URL.revokeObjectURL(url); // Clears memory
    }
}

async function exportSearchCredentialsAsCSV(credentials, params)
{
    const csvRows = [];

    csvRows.push("All Credentials Found in Search \n");

    csvRows.push("Search Parameters");
    const searchHeaders = ["Category Name", "Service Name", "User Date Choice", "Create Update Choice", "Start Date", "End Date"]
    csvRows.push(searchHeaders.join(","));
    const searchParams = [];
    for (let key in params)
    {
        searchParams.push(params[key]);
    }
    searchParams.push("\n");
    csvRows.push(searchParams.join(","));

    // Tie passwords to credentials
    const allCredentials = await getAllCredentials();
    const data = credentials.map(findUserAndPassword)

    function findUserAndPassword(credential){
        const firstElement = allCredentials.find(c => c.credentialId === credential.credentialId);
        return {
            ...credential,
            "username": firstElement.username,
            "password": firstElement.password
        }
    }

    for (let credential of data)
    {
        const credentialRow = [
            `"${credential.categoryName ?? ""}"`,
            `"${credential.serviceName ?? ""}"`,
            `"${credential.username ?? ""}"`,
            `"${credential.password ?? ""}"`,
            `"${credential.dateCreated}"`,
            `"${credential.dateLastUpdated}"`,
        ];
        csvRows.push(credentialRow.join(","));
    }

    const csvString = csvRows.join('\n');

    const blob = new Blob([csvString], { type: 'text/csv' });
    
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = 'all_credentials_found_in_search.csv';
    a.click();
    URL.revokeObjectURL(url);
}