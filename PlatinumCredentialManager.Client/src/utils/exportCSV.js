import { exportCategories } from "@/services/categoryService";
import { getAllCredentials } from "@/services/credentialService";
import { sortDecider } from "./sort";

export async function exportCSVDecider(action, sortOption = null, data, params = null){
    if (action === 'allCategories') await exportCategoriesAsCSV(sortOption);
    else if (action === 'allCredentials') await exportCredentialsAsCSV(data);
    else if (action === 'credential') await exportCredentialAsCSV(data);
    else await exportSearchCredentialsAsCSV(data, params);
};

function currentDateAndTime() {
    let currentDate = new Date();

    return currentDate.getDate() + "-" + 
    (currentDate.getMonth() + 1) + "-" +
    currentDate.getFullYear() + "_" +
    currentDate.getHours() + ":" +
    currentDate.getMinutes() + ":" +
    currentDate.getSeconds();
}

async function fileWriter(title, blob) {
    if (window.hasOwnProperty('showSaveFilePicker')){
        try{
            const fileHandle = await getSaveFilePicker(title);

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
        a.download = title;
        a.click();
        URL.revokeObjectURL(url); // Clears memory
    }
}

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
    const creationDateAndTime = currentDateAndTime();

    const csvRows = [`Date and Timestamp of Creation:, ${creationDateAndTime} \n`, "All Credentials \n"];

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

    const title = `all_categories_${currentDateAndTime}.csv`;

    await fileWriter(title, blob);
};

// Per category
async function exportCredentialsAsCSV(credentials) {
    const creationDateAndTime = currentDateAndTime();

    const csvRows = [`Date and Timestamp of Creation:, ${creationDateAndTime} \n`];

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

    const title = `all_credentials_${categoryName.toLowerCase()}_${currentDateAndTime}.csv`

    const blob = new Blob([csvString], { type: 'text/csv' });

    await fileWriter(title, blob);
}

// One credential
async function exportCredentialAsCSV(credential) 
{
    const creationDateAndTime = currentDateAndTime();

    const csvRows = [`Date and Timestamp of Creation:, ${creationDateAndTime} \n`];

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

    const title = `credential_${credential.ServiceName.toLowerCase()}_${creationDateAndTime}.csv`;

    await fileWriter(title, blob);
}

async function exportSearchCredentialsAsCSV(credentials, params)
{
    const creationDateAndTime = currentDateAndTime();

    const csvRows = [`Date and Timestamp of Creation:, ${creationDateAndTime} \n`, "All Credentials Found in Search \n", "Search Parameters"];

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

    const title = `all_credentials_found_in_search_${creationDateAndTime}.csv`

    await fileWriter(title, blob);
}