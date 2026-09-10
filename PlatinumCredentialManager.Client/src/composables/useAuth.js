import { reactive } from 'vue'
import { keycloak } from '@/config/keycloak.js'
import axios from 'axios';

const authState = reactive({
  authenticated: false,
  username:  '',
  accessToken: '',
  refreshToken: '',
  expiresAt: ''
});

const { url, realm, clientId } = keycloak;

const TOKEN_URL = `${url}/realms/${realm}/protocol/openid-connect/token`
const LOGOUT_URL = `${url}/realms/${realm}/protocol/openid-connect/logout`

async function login(username, password) {
    try {
        const { status, data } = await axios.post(TOKEN_URL, new URLSearchParams({
            grant_type: 'password',
            client_id: clientId,
            username,
            password
        }));

        // decode jwt payload
        const payload = JSON.parse(atob(data.access_token.split('.')[1]))

        // set auth state
        authState.username = payload.preferred_username
        authState.accessToken = data.access_token
        authState.refreshToken = data.refresh_token
        authState.expiresAt = Date.now() + data.expires_in * 1000
        authState.authenticated = true;

        return { status }
    }
    catch (err) {
        return {
            "status": err.response?.status,
            "error": "Invalid credentials"
        }
    }   
}

async function refresh() {
    try {
        const { data, status }  = await axios.post(TOKEN_URL, new URLSearchParams({
            grant_type: 'refresh_token',
            client_id: clientId,
            refresh_token: authState.refreshToken
        }));

        // renews session token
        authState.refreshToken = data.refresh_token;

        return {
            status
        }
    } 
    catch (err) {
        return {
            "status": err.response?.status,
            "error": "Failed to refresh token."
        }
    }
}

async function logout() {
    try {
        const { status } = await axios.post(LOGOUT_URL, new URLSearchParams({
            client_id: clientId, refresh_token: authState.refreshToken
        }));

        // Clear state
        authState.username = '';
        authState.authenticated = false;
        authState.accessToken = '';
        authState.refreshToken = '';
        authState.expiresAt = '';

        return {
            status
        }

    } catch (err) {
        return {
            "status": err.response?.status,
            "error": "Failed to logout."
        }
    }
}

export default function useAuth() {
  return { authState, login, logout, refresh }
}
