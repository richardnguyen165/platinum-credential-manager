import { createRouter, createWebHistory } from 'vue-router'
import { keycloak } from '@/config/keycloak.js'

import HomePage from '@/views/HomePage.vue';
import CredHomepage from '@/views/CredHomepage.vue';
import CredCategories from '@/views/CredCategories.vue';
import Category from '@/components/category/Category.vue';
import CredSearchup from '@/views/CredSearchup.vue';
import NotFound from '@/views/NotFound.vue';
import Credential from '@/components/credential/Credential.vue';

const router = createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', component: HomePage},
        { path: '/cred-homepage/:user_id', component: CredHomepage, meta: { requiresAuth: true }},
        { path: '/cred-categories/:user_id',  component: CredCategories, meta: { requiresAuth: true }},
        { path: '/cred-categories/:category_id/:user_id',  component: Category, meta: { requiresAuth: true }, props: route => ({ categoryId: Number(route.params.category_id), userId: Number(route.params.user_id)})},
        { path: '/cred-categories/:cred_id/:category_id/:user_id',  component: Credential, meta: { requiresAuth: true }, props: route => ({ credId: Number(route.params.cred_id), categoryId: Number(route.params.category_id), userId: Number(route.params.user_id), query: route.query })},
        { path: '/cred-searchup/:user_id', component: CredSearchup, meta: { requiresAuth: true }},
        { path: '/:pathMatch(.*)*', component: NotFound }
    ]
});

// router guard
router.beforeEach((to) => {
  if (to.meta.requiresAuth && !keycloak.authenticated) {
      keycloak.login({ redirectUri: window.location.origin + to.fullPath })
      return false
  }
});

export default router;