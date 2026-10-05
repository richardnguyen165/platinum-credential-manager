import { exportCategories } from "@/services/categoryService";
import { exportCredentials } from "@/services/credentialService";

// TODO: Implement the sorting option based on user's selection (category name)
export async function exportCategoriesAsCSV(sortOption){

    const csvRows = [];
    const data = await exportCategories();

    // https://www.geeksforgeeks.org/javascript/how-to-create-and-download-csv-file-in-javascript/

    const headers = ["Category ID", "Category Name", "Credential ID", "Service Name", "User Name", "Password", "Date Created", "Date Last Updated"];
    csvRows.push(headers.join(","));

    for (let categoryDetail of data){
        for (let credential of categoryDetail.credentials){
            // the quotes inside backticks are needed if the fields themselves have a comma
            const row = [
                categoryDetail.id,
                `"${categoryDetail.categoryName}"`,
                credential.id,
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

// TODO: Implement the sorting option based on user's selection (service name, dates)
export async function exportCredentialsAsCSV(categoryId, sortOption) {
    const csvRows = [];
    const data = await exportCredentials(categoryId);

    const headers = ["Credential ID", "Service Name", "User Name", "Password", "Date Created", "Date Last Updated"];
    csvRows.push(headers.join(","));

    for (let credential of data)
    {
        const credentialRow = [
            credential.id,
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

export async function exportCredentialAsCSV(credential) 
{
    const csvRows = [];

    const headers = ["Category Name", "Category ID", "Service ID", "Service Name", "User Name", "Password", "Date Created", "Date Last Updated"];
    csvRows.push(headers.join(","));

    const credentialData = [credential.categoryName, credential.categoryId, credential.Id, credential.serviceName, credential.username, credential.password, credential.dateCreated, credential.dateLastUpdated];
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

// TODO: Implement the sorting option based on user's selection (category name, service names, dates, match by)
export async function exportSearchCredentialsAsCSV(credentials)
{
    return;
}


