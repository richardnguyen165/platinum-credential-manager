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
  .init({ onLoad: 'login-required', pkceMethod: 'S256' })
  .then((authenticated) => {
    if (authenticated) {
      app.mount('#app')
    }
  })
  .catch((error) => {
    console.error('Keycloak initialization failed', error)
  })