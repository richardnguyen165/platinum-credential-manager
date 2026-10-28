import http from "@/interceptors/http.js"
import sendingForm from "@/interceptors/sendingForm.js"
import { type backendResponse } from "./serviceType.js";
import axios, { type AxiosResponse } from 'axios';
import type { CategoryDetail, CategoryListItem, ProblemDetails, ValidationProblemDetails } from "./responseType.js";

export async function getCategories(): Promise<backendResponse | undefined> {
    try{
        const response: AxiosResponse<CategoryListItem[], any, {}> = await http.get(`/category`);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        if (axios.isAxiosError<ProblemDetails | ValidationProblemDetails>(err)){
            console.log(err.status)
            console.error(err.response);
            return {
                data: err.response?.data,
                valid: false
            }
        } else{
            throw(err);
        }
    }
}

export async function getCategory(categoryId: string | number): Promise<backendResponse | undefined>{
    try {
        const response: AxiosResponse<CategoryDetail, any, {}>  = await http.get(`/category/${categoryId}`);
        const { data } = response;
        return {
            data,
            valid: true,
        }
    } catch (err) {
        if (axios.isAxiosError<ProblemDetails | ValidationProblemDetails>(err)){
            console.log(err.status)
            console.error(err.response);
            return {
                data: err.response?.data,
                valid: false
            }
        } else{
            throw(err);
        }
    }
}

export async function postCategory(newData: object): Promise<backendResponse | undefined> {
    try {
        const response: AxiosResponse<any, any, {}> = await http.post(`/category`, newData);
        const { data } = response;
        return {
            data,
            valid: true,
        } 
    } catch (err) {
        if (axios.isAxiosError<ProblemDetails | ValidationProblemDetails>(err)){
            console.log(err.status)
            console.error(err.response);
            return {
                data: err.response?.data,
                valid: false
            }
        } else{
            throw(err);
        }
    }
}

export async function putCategory(categoryId: string | number, changedData: object): Promise<backendResponse | undefined>{
    try {
        const response: AxiosResponse<any, any, {}> = await http.put(`/category/${categoryId}`, changedData);
        const { data } = response;
        return {
            data,
            valid: true
        }
    } catch (err) {
        if (axios.isAxiosError<ProblemDetails | ValidationProblemDetails>(err)){
            console.log(err.status)
            console.error(err.response);
            return {
                data: err.response?.data,
                valid: false
            }
        }
        else{
            throw(err);
        }
    }
}

export async function deleteCategory(categoryId: string | number): Promise<backendResponse | undefined> {
    try {
        const response: AxiosResponse<any, any, {}> = await http.delete(`/category/${categoryId}`);
        const { data } = response;
        return {
            data,
            valid: true
        }
    } catch (err) {
        if (axios.isAxiosError<ProblemDetails | ValidationProblemDetails>(err)){
            console.log(err.status)
            console.error(err.response);
            return {
                data: err.response?.data,
                valid: false
            }
        } else{
            throw(err);
        }
    }
}

export async function exportCategories(): Promise<backendResponse | undefined>{
    try {
        const response: AxiosResponse<any, any, {}> = await http.get('/category/export');
        const { data } = response;
        return {
            data,
            valid: true
        }
    } catch (err) {
        if (axios.isAxiosError<ProblemDetails | ValidationProblemDetails>(err)){
            console.log(err.status)
            console.error(err.response);
            return {
                data: err.response?.data,
                valid: false
            }
        } else{
            throw(err);
        }
    }
}

export async function importCategories(file: File): Promise<backendResponse | undefined>{

    try{
        let bodyFormData: FormData = new FormData();
        bodyFormData.append('file', file);

        const response: AxiosResponse<any, any, {}> = await sendingForm.post("/category/import",  bodyFormData);
        const { data } = response;

        return {
            data,
            valid: true
        }
    } catch (err) {
        if (axios.isAxiosError<ProblemDetails | ValidationProblemDetails>(err)){
            console.log(err.status)
            console.error(err.response);
            return {
                data: err.response?.data,
                valid: false
            }
        } else{
            throw(err);
        }
    }
}