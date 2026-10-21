import { createApp } from 'vue'
import { keycloak } from './config/keycloak.js'
import { createPinia } from 'pinia'
import Toast from 'vue-toastification'

import App from './App.vue'
import router from './router/index.js'
import 'vue-toastification/dist/index.css'

const app = createApp(App);
const pinia = createPinia();

keycloak
  .init({
    onLoad: 'check-sso',
    silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
    pkceMethod: 'S256',
    checkLoginIframe: false
  })
  .then(() => {
    /*
    Why would you move app.use(pinia) router inside.

    Reason, refreshing

    Refreshing a page wipes everything in the JS memoery, including adapter object and its tokens and check if the user's real session surives only in Keycloak's own cookie

    After a refresh, init has to redeiscover it with check-sso.

    Initally app.use(router) was outside of keycloak.init

    Since keycloak.init was asynchronous, the router started its first navigation as soon as it was installed before init has finished, so the guard logged out before confirming the user had a session
    */
    app.use(pinia);
    app.use(router);

    app.use(Toast, {
      position: 'top-right',
      timeout: 5000
    })

    app.mount('#app')
  })
  .catch((error) => {
    console.error('Keycloak initialization failed', error)
  })

// check-sso: renders the app whether or not user is logged in
// pkceMethod: 'S256' explicit PKCE
// checkLoginIframe: false, avoids third party cookie flakiness on localhost