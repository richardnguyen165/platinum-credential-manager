import axios from 'axios';
import { keycloak } from '@/config/keycloak';

// For sending a form
const sendingForm = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5142',
    headers: {
        "Content-Type": "multipart/form-data"
    }
});

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