export function sortDecider(sortFilter, withinCategorySortOption, data, credentialMatch = null){
    const copiedData = [...data];
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

function sortServiceNameAscending(data){
    data.sort((a, b) => a.serviceName.toLowerCase().localeCompare(b.serviceName.toLowerCase()));
}

function sortServiceNameDescending(data){
    data.sort((a, b) => b.serviceName.toLowerCase().localeCompare(a.serviceName.toLowerCase()));
}

function sortCategoryNameAscending(data){
    data.sort((a, b) => a.categoryName.toLowerCase().localeCompare(b.categoryName.toLowerCase()));
}

function sortCategoryNameDescending(data){
    data.sort((a, b) => b.categoryName.toLowerCase().localeCompare(a.categoryName.toLowerCase()));
}

function sortDateCreatedAscending(data){
    data.sort((a, b) => new Date(a.dateCreated) -  new Date(b.dateCreated));
}

function sortDateCreatedDescending(data){
    data.sort((a, b) => new Date(b.dateCreated) -  new Date(a.dateCreated));
}

function sortDateUpdatedAscending(data){
    data.sort((a, b) => new Date(a.dateLastUpdated) -  new Date(b.dateLastUpdated));
}

function sortDateUpdatedDescending(data){
    data.sort((a, b) => new Date(b.dateLastUpdated) -  new Date(a.dateLastUpdated));
}

function sortByMatchCredential(data, credentialMatch){
    data.sort((a, b) => {
        // use ranking system => start at the assumption they at least contain the name
        let aRank = 2, bRank = 2;

        if (a.serviceName === credentialMatch) aRank = 0;
        else if (a.serviceName.startsWith(credentialMatch)) aRank = 1;

        if (b.serviceName === credentialMatch) bRank = 0;
        else if (b.serviceName.startsWith(credentialMatch)) bRank = 1;

        // if they equal each other, 0 => evaluate right hand side
        return aRank - bRank || a.serviceName.toLowerCase().localeCompare(b.serviceName.toLowerCase());
    });
}

function sortAllCredentials(data, sortOption, secondSortOption) {
    data.sort((a, b) => {

        let firstSortCheck;

        let firstOption, secondOption;
        if (sortOption.Contains("Ascending")){
            firstOption = a;
            secondOption = b;
        } else {
            firstOption = b;
            secondOption = a;
        }
        
        firstSortCheck = firstOption.categoryName.toLowerCase().localeCompare(secondOption.categoryName.toLowerCase());
    

        if (!firstSortCheck) return firstSortCheck; // meaning they different category

        // else, they are the same category => tiebreak using the date

        if (secondSortOption.Contains("Ascending")){
            firstOption = a;
            secondOption = b;
        } else {
            firstOption = b;
            secondOption = a;
        }

        if (sortOption.Contains("Service")) return firstOption.serviceName.toLowerCase().localeCompare(secondOption.serviceName.toLowerCase());
        else if (sortOption.Contains("dateCreated")) return (new Date(firstOption.dateCreated) -  new Date(secondOption.dateCreated));
        else return (new Date(firstOption.dateLastUpdated) -  new Date(secondOption.dateLastUpdated));
    });
}