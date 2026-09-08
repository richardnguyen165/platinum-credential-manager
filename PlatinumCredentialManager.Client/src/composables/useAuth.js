import { reactive } from 'vue'
import { keycloak } from '@/config/keycloak.js'
import { urlencoded } from 'express';

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
    let result;
    try {
        result = await fetch(TOKEN_URL, { method: 'POST', body: urlencoded({ grant_type: 'password', clientId, username, password }) });

        if (!result.ok) throw new Error('failed');

        const data = await response.json(); // gets payload

        // decode jwt payload
        const payload = JSON.parse(atob(data.access_token.split('.')[1]))

        // set auth state
        authState.username = payload.preferred_username
        authState.accessToken = data.access_token
        authState.refreshToken = data.refresh_token
        authState.expiresAt = Date.now() + data.expires_in * 1000
        authState.authenticated = true;

        return {
            "status": result.status,
        }
    }
    catch {
        return {
            "status": result.status,
            "error": "Invalid credentials"
        }
    }   
}

async function refresh() {
    let result;
    try {
        result = await fetch(TOKEN_URL, { method: 'POST', body: urlencoded({ grant_type: 'refresh_token', clientId, username, password }) })

        if (!result.ok) throw new Error('failed')

        const data = await response.json(); // gets payload

        // renews session token
        authState.refreshToken = data.refresh_token;

        return {
            "status": result.status,
        }
    } 
    catch {
        return {
            "status": result.status,
            "error": "Failed to refresh token."
        }
    }
}

async function logout() {
    let result;
    try {
        result = await fetch(LOGOUT_URL, { method: 'POST', body: urlencoded({
            client_id: clientId, refresh_token: authState.refreshToken
        }) }) 

        if (!result.ok) throw new Error('failed')

        // Clear state
        authState.username = '';
        authState.authenticated = false;
        authState.accessToken = '';
        authState.refreshToken = '';
        authState.expiresAt = '';

        return {
            "status": result.status,
        }

    } catch {
        return {
            "status": result.status,
            "error": "Failed to logout."
        }
    }
}

export default function useAuth() {
  return { authState, login, logout, refresh }
}
