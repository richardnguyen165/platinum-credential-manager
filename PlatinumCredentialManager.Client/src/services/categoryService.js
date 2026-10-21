import http from "../interceptors/http";
import sendingForm from "../interceptors/sendingForm";

export async function getCategories(){
    try{
        const response = await http.get(`/category`);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function getCategory(categoryId){
    try {
        const response = await http.get(`/category/${categoryId}`);
        const { data } = response;
        return {
            data,
            valid: true,
        } 
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function postCategory(newData) {
    try {
        const response = await http.post(`/category`, newData);
        const { data } = response;
        return {
            data,
            valid: true,
        } 
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function putCategory(categoryId, changedData){
    try {
        const response = await http.put(`/category/${categoryId}`, changedData);
        const { data } = response;
        return {
            data,
            valid: true
        }
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function deleteCategory(categoryId) {
    try {
        const response = await http.delete(`/category/${categoryId}`);
        const { data } = response;
        return {
            data,
            valid: true
        }
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function exportCategories(){
    try {
        const response = await http.get('/category/export');
        const { data } = response;
        return {
            data,
            valid: true
        }
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function importCategories(file){

    try{
        let bodyFormData = new FormData();
        bodyFormData.append('file', file);

        const response = await sendingForm.post("/category/import",  bodyFormData);
        const { data } = response;

        return {
            data,
            valid: true
        }
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}