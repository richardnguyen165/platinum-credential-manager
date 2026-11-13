const SIZE: number = 10;

export function paginateData<T>(data: T[]): T[][] {
    let paginatedData = [];

    for (let i = 0; i < data.length; i += SIZE){
        paginatedData.push(data.slice(i, i + SIZE));
    }

    return paginatedData;
};