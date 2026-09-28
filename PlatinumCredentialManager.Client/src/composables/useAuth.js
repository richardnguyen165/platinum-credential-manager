import { reactive } from 'vue'
import { keycloak } from '../config/keycloak.js'
import { postNewUser } from '@/services/userService.js'

const authState = reactive({
  authenticated: keycloak.authenticated ?? false,
  username: keycloak.tokenParsed?.preferred_username ?? '',
  userId: null
});

keycloak.onAuthSuccess = async () => {
    authState.authenticated = true
    authState.username = keycloak.tokenParsed?.preferred_username ?? ''

    try {
        const { data } = await postNewUser();
        authState.userId = data.id
    } catch (err) {
        console.error('Failed to provision user', err)
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