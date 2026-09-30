import axios from 'axios';
import { keycloak } from '@/config/keycloak';

// Source: https://skycloak.io/blog/keycloak-vue-js-authentication-guide/

const http = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5142',
    headers: {
        'Content-Type': 'application/json'
    }
});

http.interceptors.request.use(
    async (config) => {
        // If authenticated stated => logged in
        if (keycloak.authenticated) {
            try {
                await keycloak.updateToken(30);
                config.headers.Authorization = `Bearer ${keycloak.token}`
            // Refresh token expired, user must refresh
            } catch {
                keycloak.logout();
            }
        }
        return config;
    },
    // responsible for catching errors
    (error) => Promise.reject(error)
);


http.interceptors.response.use((response) => response, (error) => {
    if (error.response?.status === 401) {
        keycloak.logout();
    }
    return Promise.reject(error);
});

export default http;