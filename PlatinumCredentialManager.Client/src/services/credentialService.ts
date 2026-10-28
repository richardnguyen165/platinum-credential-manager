import http from "@/interceptors/http.js"
import sendingForm from "@/interceptors/sendingForm.js"
import type { backendResponse } from "./serviceType.js";
import type { AxiosResponse } from "axios";
import type { CredentialDetail, CredentialSearchResult, ProblemDetails, ValidationProblemDetails } from "./responseType.js";
import axios from "axios";
import type { CredentialForm, SearchQuery } from "./requestType.js";

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

export async function postCredential(newData: CredentialForm): Promise<backendResponse | undefined>{
    try{
        const response: AxiosResponse<CredentialDetail, any, {}> = await http.post(`/creds`, newData);
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

export async function putCredential(credId: string | number, changedData: CredentialForm): Promise<backendResponse | undefined>{
    try{
        const response: AxiosResponse<any, any, {}> = await http.put(`/creds/${credId}`, changedData);
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

export async function deleteCredential(credId: string | number): Promise<backendResponse | undefined>{
    try {
        const response: AxiosResponse<any, any, {}> = await http.delete(`/creds/${credId}`);
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

// https://axios.rest/pages/advanced/request-config => for query parameters
export async function searchCredential(queryParams: SearchQuery){
    try {
        const response: AxiosResponse<CredentialSearchResult[], any, {}> = await http.get(`/creds/search`, { params: queryParams });
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

// https://stackoverflow.com/questions/47630163/axios-post-request-to-send-form-data
export async function importCredentials(file: File, categoryId: string | number){
    try {
        let bodyFormData: FormData = new FormData();
        bodyFormData.append('file', file);

        const response: AxiosResponse<any, any, {}> = await sendingForm.post(`/creds/import/${categoryId}`, bodyFormData);
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