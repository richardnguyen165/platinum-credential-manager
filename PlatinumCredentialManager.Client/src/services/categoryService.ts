import http from "@/interceptors/http.js"
import sendingForm from "@/interceptors/sendingForm.js"
import { type backendResponse } from "./serviceTypes.js";

export async function getCategories(): Promise<backendResponse>{
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

export async function getCategory(categoryId: string | number): Promise<backendResponse>{
    try {
        const response: any = await http.get(`/category/${categoryId}`);
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

export async function postCategory(newData: object): Promise<backendResponse> {
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

export async function putCategory(categoryId: string | number, changedData: object): Promise<backendResponse>{
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

export async function deleteCategory(categoryId: string | number): Promise<backendResponse> {
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

export async function exportCategories(): Promise<backendResponse>{
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

export async function importCategories(file: File): Promise<backendResponse>{

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