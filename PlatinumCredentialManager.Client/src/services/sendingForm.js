import axios from 'axios';
import { keycloak } from '@/config/keycloak';
import http from "./http";

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
const sendingForm = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5142',
    // https://stackoverflow.com/questions/43002444/make-axios-send-cookies-in-its-requests-automatically
    withCredentials: true,
    headers: {
        "Content-Type": "multipart/form-data"
    }
});

sendingForm.interceptors.request.use(
    async (config) => {
        config.headers["X-CSRF-TOKEN"] = await antiForgeryCookie();
        return config;
    },
    (error) => Promise.reject(error)
);

sendingForm.interceptors.request.use(
    async (config) => {
        // If authenticated stated => logged in
        if (keycloak.authenticated) {
            try {
                await keycloak.updateToken(30);
                config.headers.Authorization = `Bearer ${keycloak.token}`
            // Refresh token expired, user must refresh
            } catch {
                console.log('User refresh token expired, logging out user...')
                keycloak.logout();
            }
        }
        return config;
    },
    // responsible for catching errors
    (error) => Promise.reject(error)
);


sendingForm.interceptors.response.use((response) => response, (error) => {
    if (error.response?.status === 401) {
        keycloak.logout();
    }
    return Promise.reject(error);
});

export default sendingForm;