import { reactive } from 'vue'
import { useToast } from 'vue-toastification';

import { keycloak } from '@/config/keycloak'
import { serviceHandler } from '@/services/serviceHandler.js';

const authState = reactive({
  authenticated: keycloak.authenticated ?? false,
  username: keycloak.tokenParsed?.preferred_username ?? '',
  userId: null
});

keycloak.onAuthSuccess = async () => {
    authState.authenticated = true
    authState.username = keycloak.tokenParsed?.preferred_username ?? ''

    try {
        const response = await serviceHandler('post-new-user');
        if (response["valid"]){
            const { data }= response;
            authState.userId = data.id;
        }
        else {
            useToast().error("Error logging in: Logging out");
            console.log("Log in crash...")
            console.log(`Error Title: ${response?.title ?? "Error"}`);
            console.log(`Error Code: ${response?.status ?? "Status Unknown"}`);
            console.log(`Error Details: ${response?.detail ?? "Details Unknown"}`)
            keycloak.logout();
        }
    } catch (error) {
        console.error('Failed to provision user', error)
    }
}

keycloak.onAuthLogout = () => {
    authState.authenticated = false
    authState.username = ''
}

keycloak.onTokenExpired = () => {
    keycloak.updateToken(30).catch(() => keycloak.login());
}

function signup() {
    // Goes to homepage
    keycloak.register({ redirectUri: window.location.origin + '/' })
}

function login() {
    keycloak.login()
}

function logout() {
    // Goes to homepage
    keycloak.logout({ redirectUri: window.location.origin + '/' })
}

export default function useAuth() {
  return { keycloak, authState, login, logout, signup }
}