import { createApp } from 'vue'
import App from './App.vue'
import { keycloak } from './config/keycloak.js'
import { createRouter, createWebHistory } from 'vue-router'
import LogIn from './views/LogIn.vue'
import SignUp from './views/SignUp.vue'
import HomePage from './views/HomePage.vue'
import CredHomepage from './views/CredHomepage.vue'
import CredCategories from './views/CredCategories.vue'
import CredSearchup from './views/CredSearchup.vue'

const router = createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', component: HomePage},
        { path: '/signup', component: SignUp},
        { path: '/login', component: LogIn},
        { path: '/cred-homepage/:user_id', query: {user_id: Number}, component: CredHomepage, meta: { requiresAuth: true }},
        { path: '/cred-categories/:user_id', query: {user_id: Number},  component: CredCategories, meta: { requiresAuth: true }},
        { path: '/cred-categories/:category_id/:user_id', query: {category_id: Number, user_id: Number},  component: CredCategories, meta: { requiresAuth: true }},
        { path: '/cred-categories/:cred_id/:category_id/:user_id', query: {cred_id: Number, category_id: Number, user_id: Number},  component: CredCategories, meta: { requiresAuth: true }},
        { path: '/cred-searchup/:user_id', query: {user_id: Number}, component: CredSearchup, meta: { requiresAuth: true }},
        { path: '/cred-searchup/:user_id/:search', query: {user_id: Number, search: String}, component: CredSearchup, meta: { requiresAuth: true }},
        { path: '/:pathMatch(.*)*', component: NotFound }
    ]
});

// router guard
router.beforeEach((to) => {
  if (to.meta.requiresAuth && !keycloak.authenticated) {
      keycloak.login({ redirectUri: window.location.orgin + to.fullPath })
      return false
  }
})

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
