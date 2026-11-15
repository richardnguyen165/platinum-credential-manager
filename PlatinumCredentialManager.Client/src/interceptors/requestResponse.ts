import { keycloak } from '@/config/keycloak.js';
import type { InternalAxiosRequestConfig } from 'axios';
import axios from 'axios';

export function createAxios(action: string){
    if (action === 'sendingForm'){
        return {
            baseURL: import.meta.env.VITE_API_URL || 'https://localhost:7032',
            // https://stackoverflow.com/questions/43002444/make-axios-send-cookies-in-its-requests-automatically
            withCredentials: true,
            headers: {
                "Content-Type": "multipart/form-data"
        }
    }
    } else {
        return {
                baseURL: import.meta.env.VITE_API_URL || 'https://localhost:7032',
                withCredentials: true, // needed to send cookie
                headers: {
                    'Content-Type': 'application/json'
                }
            }
    }
}

export async function createConfig(config: InternalAxiosRequestConfig){
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
}

export async function errorResponse(error: unknown){
    if (axios.isAxiosError(error)){
        if (error.response?.status === 401) {
            keycloak.logout();
        }
    }
    return Promise.reject(error);
}