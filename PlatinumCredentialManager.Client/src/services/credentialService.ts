import http from "@/interceptors/http.js"
import sendingForm from "@/interceptors/sendingForm.js"
import type { backendResponse } from "./serviceType.js";
import type { AxiosResponse } from "axios";
import type { CredentialDetail, ProblemDetails, ValidationProblemDetails } from "./responseType.js";
import axios from "axios";

export async function getAllCredentials(): Promise<backendResponse | undefined> {
    try{
        const response: AxiosResponse<CredentialDetail[], any, {}> = await http.get(`/creds`);
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

export async function getCredential(credId: string | number): Promise<backendResponse | undefined>{
    try{
        const response: AxiosResponse<CredentialDetail, any, {}> = await http.get(`/creds/${credId}`);
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

export async function postCredential(newData) {
    try{
        const response = await http.post(`/creds`, newData);
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

export async function putCredential(credId: string | number, changedData){
    try{
        const response = await http.put(`/creds/${credId}`, changedData);
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

export async function deleteCredential(credId: string | number) {
    try {
        const response = await http.delete(`/creds/${credId}`);
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

// https://axios.rest/pages/advanced/request-config => for query parameters
export async function searchCredential(queryParams){
    try {
        const response = await http.get(`/creds/search`, { params: queryParams });
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

// https://stackoverflow.com/questions/47630163/axios-post-request-to-send-form-data
export async function importCredentials(file: File, categoryId: string | number){
    try {
        let bodyFormData = new FormData();
        bodyFormData.append('file', file);

        const response = await sendingForm.post(`/creds/import/${categoryId}`, bodyFormData);
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