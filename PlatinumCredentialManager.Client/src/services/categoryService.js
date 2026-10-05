import http from "./http";

export async function getCategories(){
    const { data } = await http.get(`/category`);
    return data;
}

export async function getCategory(categoryId){
    const { data } = await http.get(`/category/${categoryId}`);
    return data;
}

export async function postCategory(newData) {
    const { data } = await http.post(`/category`, newData);
    return data;
}

export async function putCategory(categoryId, changedData){
    const { data } = await http.put(`/category/${categoryId}`, changedData);
    return data;
}

export async function deleteCategory(categoryId) {
    const { data } = await http.delete(`/category/${categoryId}`);
    return data;
}

export async function exportCategories(){
    const { data } = await http.get('/category/export');
    return data;
}