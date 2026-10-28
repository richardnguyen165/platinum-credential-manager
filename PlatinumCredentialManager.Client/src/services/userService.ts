import http from "@/interceptors/http.js"
import axios, { type AxiosResponse } from "axios";
import type { ProblemDetails, UserProfile, ValidationProblemDetails } from "./responseType.js";
import type { backendResponse } from "./serviceType.js";

export async function postNewUser(): Promise<backendResponse | undefined>{
    // Logic of no try and catch block, let the calling parent throw it
    try{
        const response: AxiosResponse<UserProfile, any, {}> = await http.post(`/user/me`);
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

export async function getUser(): Promise<backendResponse | undefined>{
    try{
        const response: AxiosResponse<UserProfile, any, {}> = await http.get(`/user/me`);
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