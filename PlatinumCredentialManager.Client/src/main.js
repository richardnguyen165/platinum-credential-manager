import { createRouter, createWebHistory } from 'vue-router'
import { createApp } from 'vue'
import { keycloak } from './config/keycloak.js'

import App from './App.vue'
import HomePage from './views/HomePage.vue'
import CredHomepage from './views/CredHomepage.vue'
import CredCategories from './views/CredCategories.vue'
import CredSearchup from './views/CredSearchup.vue'
import NotFound from './views/NotFound.vue'
import Category from './components/category/Category.vue'
import Credential from './components/credential/Credential.vue'

const router = createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', component: HomePage},
        { path: '/cred-homepage/:user_id', component: CredHomepage, meta: { requiresAuth: true }},
        { path: '/cred-categories/:user_id',  component: CredCategories, meta: { requiresAuth: true }},
        { path: '/cred-categories/:category_id/:user_id',  component: Category, meta: { requiresAuth: true }, props: route => ({ categoryId: Number(route.params.category_id), userId: Number(route.params.user_id)})},
        { path: '/cred-categories/:cred_id/:category_id/:user_id',  component: Credential, meta: { requiresAuth: true }, props: route => ({ credId: Number(route.params.cred_id), categoryId: Number(route.params.category_id), userId: Number(route.params.user_id)})},
        { path: '/cred-searchup/:user_id', component: CredSearchup, meta: { requiresAuth: true }},
        { path: '/cred-searchup/:user_id/:search', component: CredSearchup, meta: { requiresAuth: true }},
        { path: '/:pathMatch(.*)*', component: NotFound }
    ]
});

// router guard
router.beforeEach((to) => {
  if (to.meta.requiresAuth && !keycloak.authenticated) {
      keycloak.login({ redirectUri: window.location.origin + to.fullPath })
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
