import { reactive } from 'vue'
import { keycloak } from '../config/keycloak.js'

const authState = reactive({
  authenticated: keycloak.authenticated ?? false,
  username: keycloak.tokenParsed?.preferred_username ?? '',
})

keycloak.onAuthSuccess = () => {
    authState.authenticated = true
    authState.username = keycloak.tokenParsed?.preferred_username ?? ''
}

keycloak.onAuthLogout = () => {
    authState.authenticated = false
    authState.username = ''
}

keycloak.onTokenExpired = () => {
    keycloak.updateToken(30);
}

//  Redirec to keycloak's login page
function login() {
    keycloak.login()
}

function logout() {
    keycloak.logout()
}

export default function useAuth() {
  return { authState, login, logout }
}
