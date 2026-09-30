<template>
  <h1>Welcome to Platinum Credential Manager - Home</h1>
  <button @click="goToSignUp">Sign Up</button>
  <button @click="goToLogIn">Log In</button>
</template>

<script setup>
    import { watch } from 'vue'
    import { useRouter } from 'vue-router'
    
    import useAuth from '@/composables/useAuth'
    import { homepagePath } from '@/utils/routes';

    const { authState, login, signup } = useAuth();

    const router = useRouter();

    // If userId in authState is defined (meaning successful request), redirect to user's homepage
    watch(() => authState.userId, (userId) => {
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