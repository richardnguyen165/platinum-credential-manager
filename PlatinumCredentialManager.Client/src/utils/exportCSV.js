import { exportCategories } from "@/services/categoryService";
import { getAllCredentials } from "@/services/credentialService";

export async function exportCSVDecider(action, sortOption){
    if (action === 'allCategories') exportCategoriesAsCSV(sortOption);
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
    
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = 'all_categories.csv';
    a.click();
    URL.revokeObjectURL(url); // Clears memory
};

// Per category
async function exportCredentialsAsCSV(credentials) {
    const csvRows = [];

    csvRows.push(`All Credentials in ${categoryName} \n`);

    const allCredentials = await getAllCredentials().map(findPassword);

    function findPassword(credential){
        const firstElement = allCredentials.find(c => c.credentialId === credential.credentialId);
        return {
            ...credential,
            "password": firstElement.password
        }
    }

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

    const credentialData = [credential.categoryName, credential.serviceName, credential.username, credential.password, credential.dateCreated, credential.dateLastUpdated];
    csvRows.push(credentialData.join(","));

    const csvString = csvRows.join('\n');

    const blob = new Blob([csvString], { type: 'text/csv' });
    
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = `credential_${credential.serviceName}.csv`;
    a.click();
    URL.revokeObjectURL(url);
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


