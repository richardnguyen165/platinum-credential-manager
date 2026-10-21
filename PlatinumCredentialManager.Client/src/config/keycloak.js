import Keycloak from 'keycloak-js'

export const keycloak = new Keycloak({
    url: import.meta.env.VITE_KEYCLOAK_URL ?? 'https://localhost:8443',
    realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'platinum',
    clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'platinum-vue'
});