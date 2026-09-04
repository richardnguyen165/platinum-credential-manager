import { createApp } from 'vue'
import App from './App.vue'
import { keycloak } from './config/keycloak.js'
import { createRouter, createWebHistory } from 'vue-router'
import LogIn from './views/LogIn.vue'
import SignUp from './views/SignUp.vue'
import HomePage from './views/HomePage.vue'

const router = createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', component: HomePage},
        { path: '/signup', component: SignUp},
        { path: '/login', component: LogIn}
    ]
});

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
