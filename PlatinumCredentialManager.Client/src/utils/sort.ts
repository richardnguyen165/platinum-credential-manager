export type sortAction = 'azAscendingService' | 'azDescendingService' | 'azAscendingCategory' | 'azDescendingCategory' | 'dateCreatedAscending' | 'dateCreatedDescending' | 'dateUpdatedAscending' | 'dateUpdatedDescending' | 'searchup'

export interface DataRow {
    serviceName?: string;
    categoryName?: string;
    dateCreated?: string;
    dateLastUpdated?: string;
}

export function sortDecider(sortFilter: sortAction, withinCategorySortOption: string | null, data: DataRow[], credentialMatch: string = ""){
    const copiedData: DataRow[] = [...data];
    // This is for all options
    if (sortFilter && withinCategorySortOption) sortAllCredentials(copiedData, sortFilter, withinCategorySortOption);
    else if (sortFilter === "azAscendingService") sortServiceNameAscending(copiedData);
    else if (sortFilter === "azDescendingService") sortServiceNameDescending(copiedData);
    else if (sortFilter === "azAscendingCategory") sortCategoryNameAscending(copiedData);
    else if (sortFilter === "azDescendingCategory") sortCategoryNameDescending(copiedData);
    else if (sortFilter === "dateCreatedAscending") sortDateCreatedAscending(copiedData);
    else if (sortFilter === "dateCreatedDescending") sortDateCreatedDescending(copiedData);
    else if (sortFilter === "dateUpdatedAscending") sortDateUpdatedAscending(copiedData);
    else if (sortFilter === "dateUpdatedDescending") sortDateUpdatedDescending(copiedData);
    else if (sortFilter === "searchup") sortByMatchCredential(copiedData, credentialMatch);
    return copiedData;
};

// https://www.geeksforgeeks.org/javascript/sort-an-array-of-strings-in-javascript/

// Shared comparators. A missing field is treated as "" / the epoch, so a row
// lacking the sorted field orders first ascending instead of crashing.
const byText = (x = "", y = "") => x.toLowerCase().localeCompare(y.toLowerCase());

const byDate = (x?: string, y?: string) =>
    (x ? new Date(x).getTime() : 0) - (y ? new Date(y).getTime() : 0);

function sortServiceNameAscending(data: DataRow[]){
    data.sort((a, b) => byText(a.serviceName, b.serviceName));
}

function sortServiceNameDescending(data: DataRow[]){
    data.sort((a, b) => byText(b.serviceName, a.serviceName));
}

function sortCategoryNameAscending(data: DataRow[]){
    data.sort((a, b) => byText(a.categoryName, b.categoryName));
}

function sortCategoryNameDescending(data: DataRow[]){
    data.sort((a, b) => byText(b.categoryName, a.categoryName));
}

function sortDateCreatedAscending(data: DataRow[]){
    data.sort((a, b) => byDate(a.dateCreated, b.dateCreated));
}

function sortDateCreatedDescending(data: DataRow[]){
    data.sort((a, b) => byDate(b.dateCreated, a.dateCreated));
}

function sortDateUpdatedAscending(data: DataRow[]){
    data.sort((a, b) => byDate(a.dateLastUpdated, b.dateLastUpdated));
}

function sortDateUpdatedDescending(data: DataRow[]){
    data.sort((a, b) => byDate(b.dateLastUpdated, a.dateLastUpdated));
}

function sortByMatchCredential(data: DataRow[], credentialMatch: string){
    data.sort((a, b) => {
        // use ranking system => start at the assumption they at least contain the name
        let aRank = 2, bRank = 2;

        if (a.serviceName === credentialMatch) aRank = 0;
        else if (a.serviceName.startsWith(credentialMatch)) aRank = 1;

        if (b.serviceName === credentialMatch) bRank = 0;
        else if (b.serviceName.startsWith(credentialMatch)) bRank = 1;

        // if they equal each other, 0 => evaluate right hand side
        return aRank - bRank || byText(a.serviceName, b.serviceName);
    });
}

function sortAllCredentials(data: DataRow[], sortOption: string, secondSortOption: string) {
    data.sort((a, b) => {

        let firstSortCheck;

        let firstOption, secondOption;
        if (sortOption.includes("Ascending")){
            firstOption = a;
            secondOption = b;
        } else {
            firstOption = b;
            secondOption = a;
        }
        
        firstSortCheck = byText(firstOption.categoryName, secondOption.categoryName);
    

        if (firstSortCheck !== 0) return firstSortCheck; // meaning they different category

        // else, they are the same category => tiebreak using the date

        if (secondSortOption.includes("Ascending")){
            firstOption = a;
            secondOption = b;
        } else {
            firstOption = b;
            secondOption = a;
        }

        if (secondSortOption.includes("Service")) return byText(firstOption.serviceName, secondOption.serviceName);
        else if (secondSortOption.includes("dateCreated")) return byDate(firstOption.dateCreated, secondOption.dateCreated);
        else return byDate(firstOption.dateLastUpdated, secondOption.dateLastUpdated);
    });
}