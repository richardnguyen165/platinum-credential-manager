import { createRouter, createWebHistory, type RouteLocationNormalizedGeneric, type Router } from 'vue-router'
import { keycloak } from '@/config/keycloak.js'

import WelcomePage from '@/views/WelcomePage.vue';
import AppHomepage from '@/views/AppHomepage.vue';
import AppCategories from '@/views/AppCategories.vue';
import AppCategory from '@/components/category/AppCategory.vue';
import AppSearchup from '@/views/AppSearchup.vue';
import AppCredential from '@/components/credential/AppCredential.vue';
import NotFound from '@/views/NotFound.vue';

const router: Router = createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', component: WelcomePage},
        { path: '/cred-homepage/:user_id', component: AppHomepage, meta: { requiresAuth: true }},
        { path: '/cred-categories/:user_id',  component: AppCategories, meta: { requiresAuth: true }},
        { path: '/cred-categories/:category_id/:user_id',  component: AppCategory, meta: { requiresAuth: true }},
        { path: '/cred-categories/:cred_id/:category_id/:user_id',  component: AppCredential, meta: { requiresAuth: true }, props: route => ({ query: route.query })},
        { path: '/cred-searchup/:user_id', component: AppSearchup, meta: { requiresAuth: true }},
        { path: '/:pathMatch(.*)*', component: NotFound }
    ]
});

// router guard
router.beforeEach((to: RouteLocationNormalizedGeneric) => {
  if (to.meta.requiresAuth && !keycloak.authenticated) {
      keycloak.login()
  }
});

export default router;