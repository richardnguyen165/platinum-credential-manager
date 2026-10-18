import { createApp } from 'vue'
import { keycloak } from './config/keycloak.js'
import { createPinia } from 'pinia'
import Toast from 'vue-toastification'

import App from './App.vue'
import router from './router/index.js'
import 'vue-toastification/dist/index.css'

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);

app.use(Toast, {
  position: 'top-right',
  timeout: 5000
})

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
