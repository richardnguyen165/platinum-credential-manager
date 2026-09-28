<script setup></script>

<template>
  <h1>Welcome to Platinum Credential Manager - Home</h1>
  <button @click="goToSignUp">Sign Up</button>
  <button @click="goToLogIn">Log In</button>
</template>

<style scoped></style>

<script setup>
    import { watch } from 'vue'
    import { useRouter } from 'vue-router'
    import useAuth from '@/composables/useAuth'

    const { keycloak, authState, login, logout, signup } = useAuth();

    const router = useRouter();

    watch(() => authState.userId, (userId) => {
        if (userId) router.push(`/cred-homepage/${userId}`)
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