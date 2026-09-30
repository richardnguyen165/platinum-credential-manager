import { createApp } from 'vue'
import { keycloak } from './config/keycloak.js'

import App from './App.vue'
import router from './router/index.js'

const app = createApp(App);

app.use(router);

keycloak
  .init({
    onLoad: 'check-sso',
    silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
    pkceMethod: 'S256',
    checkLoginIframe: false
  })
  .then(() => {
    app.mount('#app')
  })
  .catch((error) => {
    console.error('Keycloak initialization failed', error)
  })

// check-sso: renders the app whether or not user is logged in
// pkceMethod: 'S256' explicit PKCE
// checkLoginIframe: false, avoids third party cookie flakiness on localhost
