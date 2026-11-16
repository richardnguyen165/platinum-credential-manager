<template>
  <h1>Welcome to Platinum Credential Manager - Home</h1>
  <button @click="goToSignUp">Sign Up</button>
  <button @click="goToLogIn">Log In</button>
</template>

<script setup lang="ts">
    import { watch } from 'vue'
    import { useRouter, type Router } from 'vue-router'
    
    import useAuth from '@/composables/useAuth.js'
    import { homepagePath } from '@/router/routes.js';

    const { authState, login, signup } = useAuth();

    const router: Router = useRouter();

    // If userId in authState is defined (meaning successful request), redirect to user's homepage
    watch(() => authState.userId, (userId: number | null) => {
        if (userId) router.push(homepagePath(userId))
    }, { immediate: true })

    // Redirects
    function goToLogIn() {
        // router.push('/login');
        login();
    }

    function goToSignUp() {
        // router.push('/signup');
        signup();
    }
</script>