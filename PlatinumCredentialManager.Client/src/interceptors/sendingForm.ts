import axios, { type AxiosInstance } from 'axios';
import http from "@/interceptors/http.js"
import { createAxios, createConfig, errorResponse } from '@/interceptors/requestResponse.js';

export async function antiForgeryCookie(): Promise<string | undefined>{
    try{
        await http.get(`/antiforgery/token`);

        const row: string | undefined = document.cookie.split("; ").find(r => r.startsWith("X-CSRF-TOKEN="));
        if (!row) throw new Error("Antiforgery cookie not set");
        
        const antiForgeryCookie: string | undefined  = row.split("=")[1];

        return antiForgeryCookie;
    } catch (err) {
        console.log("Request Failed")
        return undefined;
    }
}

// For sending a form
const sendingForm: AxiosInstance = axios.create(createAxios('sendingForm'));

sendingForm.interceptors.request.use(
    async (config) => {
        config.headers["X-CSRF-TOKEN"] = await antiForgeryCookie();
        return config;
    },
    (error) => Promise.reject(error)
);

sendingForm.interceptors.request.use(
    async (config) => await createConfig(config),
    // responsible for catching errors
    (error) => Promise.reject(error)
);


sendingForm.interceptors.response.use((response) => response, (error) => errorResponse(error));

export default sendingForm;