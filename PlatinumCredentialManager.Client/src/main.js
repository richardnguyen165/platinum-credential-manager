import { createApp } from 'vue'
import App from './App.vue'
import keycloak from './config/keycloak.js'

keycloak.init({
    onLoad: 'check-sso',
    silentCheckSsoRedirectUri: window.location.orgin + '/silent-check-sso.html',
    pkceMethod: 'S256',
    checkLoginIframe: false
}).then(() => createApp(App).mount('#app'))

// check-sso: renders the app wether or not user is logged in
// pkceMethod: 'S256' explictPKCE
// checkLoginIframe: false, avoids third part cookie flakiness on localhost