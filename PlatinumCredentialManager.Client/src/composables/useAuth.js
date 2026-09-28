import { reactive } from 'vue'
import { keycloak } from '../config/keycloak.js'
import axios from 'axios'

const authState = reactive({
  authenticated: keycloak.authenticated ?? false,
  username: keycloak.tokenParsed?.preferred_username ?? '',
  userId: null
})

const BACKEND_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5142';

keycloak.onAuthSuccess = async () => {
    authState.authenticated = true
    authState.username = keycloak.tokenParsed?.preferred_username ?? ''

    try {
        const { data } = await axios.post(`${BACKEND_URL}/user/me`, null, {
        headers: { Authorization: `Bearer ${keycloak.token}` }});

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