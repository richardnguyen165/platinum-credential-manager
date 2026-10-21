import { keycloak } from '@/config/keycloak';

export function createAxios(action){
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

export async function createConfig(config){
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

export async function errorResponse(error){
    if (error.response?.status === 401) {
        keycloak.logout();
    }
    return Promise.reject(error);
}