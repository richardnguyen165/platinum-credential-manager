import { useToast } from 'vue-toastification';

import { sortDecider } from "@/utils/sort.js";
import { serviceHandler } from "@/services/serviceHandler.js";
import { ExportError } from "@/csv/ExportError.js";
import type { GridData, SearchParams } from '@/types/gridType.js';
import type { DataRow, SortAction } from '@/types/sortType.js';
import type { SearchQuery, ServiceCall } from '@/types/requestType.js';
import type { HandlerResult } from '@/types/serviceType.js';
import type { CredentialDetail, CredentialSearchResult, CredentialSummary } from '@/types/responseType.js';
import type { CurrentEntries } from '@/types/formType.js';

const SEARCH_KEYS = ["CategoryName", "ServiceName", "UserDateChoice", "CreateUpdateChoice", "StartDate", "EndDate"] as const satisfies readonly (keyof SearchParams)[];

export async function exportCSVDecider(csvPayload: GridData, sortOption: SortAction, withinCategorySortOption: string | null){
    try {
        if (csvPayload.location === 'allCategories') await exportCategoriesAsCSV(sortOption, withinCategorySortOption);
        else if (csvPayload.location === 'allCredentials') await exportCredentialsAsCSV(csvPayload.rows);
        else if (csvPayload.location === 'credential') await exportCredentialAsCSV(csvPayload.rows);
        else await exportSearchCredentialsAsCSV(csvPayload.rows, csvPayload.params);
    } catch (error) {
        const toast = useToast();
        if (error instanceof ExportError){
            toast.error(error.message);
        } else if (error instanceof Error) {
            toast.warning(error.message);
        } else { // unexpected error
            console.error('Unexpected export failure', error);
            toast.warning("Export failed. Please try again.");
        }
    }
};

function currentDateAndTime(): string {
    let currentDate: Date = new Date();
    let currentDateString: string = `${currentDate.getDate()}-${currentDate.getMonth() + 1}-${currentDate.getFullYear()}_${currentDate.getHours()}-${currentDate.getMinutes()}-${currentDate.getSeconds()}`

    return currentDateString;
}

async function fileWriter(title: string, blob: Blob) {
    if (window.hasOwnProperty('showSaveFilePicker')){
        let writable: FileSystemWritableFileStream | undefined;
        try{
            const fileHandle = await getSaveFilePicker(title);

            // https://developer.mozilla.org/en-US/docs/Web/API/FileSystemFileHandle
            writable = await fileHandle.createWritable();

            await writable.write(blob);
        } catch (error) {
            if (!(error instanceof DOMException)) throw error;   // TypeError etc. pass straight through

            if (error.name === 'AbortError'){
                const toast = useToast();
                toast.info("Cancelling export.")
                // will implement a toast here - this is not supposed to be an error
                return;
            } else if (error.name === 'SecurityError'){
                throw new ExportError("File picker opened for too long, or file picker opened from insecure page")
            } else if (error.name === 'NotAllowedError'){
                throw new ExportError("Permission to write to file denied");
            } else if (error.name === "NotAllowedError"){
                throw new ExportError("Browser or user blocked the picker");
            } else if (error.name === "NoModificationAllowedError"){
                throw new ExportError("File is locked or in use."); 
            } else if  (error.name === "QuotaExceededError") {
                throw new ExportError("Disk is full or storage limit hit");
            } else if (error.name === 'TypeError'){
                throw error;
            } else {
                throw error;
            }
        } finally {
            await writable?.close();
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
async function getSaveFilePicker(fileName: string): Promise<FileSystemFileHandle> {
    const opts: SaveFilePickerOptions  = {
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
async function exportCategoriesAsCSV(sortOption: SortAction, withinCategorySortOption: string | null){
    const creationDateAndTime = currentDateAndTime();

    const csvRows = [`Date and Timestamp of Creation:, ${creationDateAndTime} \n`, "All Credentials \n"];

    const payload: ServiceCall = {
        action: 'export-categories'
    }

    const { valid, data } = await serviceHandler(payload);

    if (!valid || !data) {
        throw new ExportError('Error exporting CSV');
    }

    const flattenedCredentials = []

    for (const category of data){
        let categoryName = category.categoryName;
        for (const credential of category.credentials){
            flattenedCredentials.push({
                ...credential,
                categoryName
            })
        }
    }
    
    const sortedCrdentials: DataRow[] = sortDecider(sortOption, withinCategorySortOption, flattenedCredentials);

    // https://www.geeksforgeeks.org/javascript/how-to-create-and-download-csv-file-in-javascript/

    const headers = ["Category Name", "Service Name", "User Name", "Password", "Date Created", "Date Last Updated"];
    csvRows.push(headers.join(","));

    for (let credential of sortedCrdentials){
        // the quotes inside backticks are needed if the fields themselves have a comma
        const row = [
            `"${credential.categoryName}"`,
            `"${credential.serviceName ?? ""}"`,
            `"${credential.username ?? ""}"`,
            `"${credential.password ?? ""}"`,
            `"${credential.dateCreated}"`,
            `"${credential.dateLastUpdated}"`,
        ];
        csvRows.push(row.join(","));
    }

    const csvString = csvRows.join('\n');

    const blob = new Blob([csvString], { type: 'text/csv' });

    const title = `all_categories_${creationDateAndTime}.csv`;

    await fileWriter(title, blob);
};

// Per category
async function exportCredentialsAsCSV(credentials: CredentialSummary[]) {
    const creationDateAndTime = currentDateAndTime();

    const csvRows = [`Date and Timestamp of Creation:, ${creationDateAndTime} \n`];

    const payload: ServiceCall = {
        action: 'getAllCredentials'
    }

    const response: HandlerResult<CredentialDetail[]> = await serviceHandler(payload);

    if (!response.valid){
        throw new ExportError(`${response.data?.title ?? "Error"}\n${response.data?.detail ?? ""}`);
    }
    const allCredentials: CredentialDetail[] = response.data;

    const data = credentials.map(findPassword);

    function findPassword(credential: CredentialSummary){
        const firstElement = allCredentials.find(c => c.id === credential.id);
        if (!firstElement) throw new ExportError(`Credential ${credential.id} not found`);
        return {
            ...credential,
            "password": firstElement.password,
            "categoryName": firstElement.categoryName
        }
    }

    if (data == null || data == undefined) throw new ExportError("Error in exporting credentials: one credential does not exist");

    const first = data[0];
    if (!first) throw new ExportError("No credentials to export in this category");

    let categoryName = first.categoryName;

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

    const title = `all_credentials_${categoryName.toLowerCase()}_${creationDateAndTime}.csv`

    const blob = new Blob([csvString], { type: 'text/csv' });

    await fileWriter(title, blob);
}

// One credential
async function exportCredentialAsCSV(credential: CurrentEntries) 
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

    const title = `credential_${credential.ServiceName?.toLowerCase() ?? "credential"}_${creationDateAndTime}.csv`;

    await fileWriter(title, blob);
}

async function exportSearchCredentialsAsCSV(credentials: CredentialSearchResult[], params: Partial<SearchQuery>)
{
    const creationDateAndTime = currentDateAndTime();

    const csvRows = [`Date and Timestamp of Creation:, ${creationDateAndTime} \n`, "All Credentials Found in Search \n", "Search Parameters"];

    const searchHeaders = ["Category Name", "Service Name", "User Date Choice", "Create Update Choice", "Start Date", "End Date"]
    csvRows.push(searchHeaders.join(","));
    const searchParams = [];
    for (const key of SEARCH_KEYS)
    {
        searchParams.push(params[key] ?? "");
    }
    searchParams.push("\n");
    csvRows.push(searchParams.join(","));

    const payload: ServiceCall = {
        action: 'getAllCredentials'
    }

    // Tie passwords to credentials
    const response: HandlerResult<CredentialDetail[]> = await serviceHandler(payload);

    if (!response.valid){
        throw new ExportError(`${response.data?.title ?? "Error"}\n${response.data?.detail ?? ""}`);
    }

    const allCredentials: CredentialDetail[] = response.data;

    const data = credentials.map(findUserAndPassword)

    function findUserAndPassword(credential: CredentialSearchResult){
        const firstElement = allCredentials.find(c => c.id === credential.credentialId);
        return {
            ...credential,
            "username": firstElement?.username,
            "password": firstElement?.password
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

    const blob: Blob = new Blob([csvString], { type: 'text/csv' });

    const title = `all_credentials_found_in_search_${creationDateAndTime}.csv`

    await fileWriter(title, blob);
}