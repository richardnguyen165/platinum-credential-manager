const SIZE = 10;

export function paginateData(data) {
    let paginatedData = [];

    for (let i = 0; i < data.length; i += SIZE){
        paginatedData.push(data.slice(i, i + SIZE));
    }

    return paginatedData;
};