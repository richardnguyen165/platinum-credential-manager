import axios from 'axios';
import http from "@/interceptors/http"
import { createAxios, createConfig, errorResponse } from '@/interceptors/requestResponse';

export async function antiForgeryCookie(){
    try{
        const response = await http.get(`/antiforgery/token`);
        const antiForgeryCookie = document.cookie
            .split("; ")
            .find(row => row.startsWith("X-CSRF-TOKEN="))
            .split("=")[1];

        return antiForgeryCookie;
    } catch (err) {
        console.log("Request Failed")
        return null;
    }
}

// For sending a form
const sendingForm = axios.create(createAxios('sendingForm'));

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