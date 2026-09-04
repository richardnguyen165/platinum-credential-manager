<script setup></script>

<template>
  <h1>Welcome to Platinum Credential Manager - Home</h1>
  <button @click="goToSignUp">Sign Up</button>
  <button @click="goToLogIn">Log In</button>
</template>

<style scoped></style>

<script setup>
    import  { useRouter } from 'vue-router'
    
    const router = useRouter();

    // Redirects
    function goToLogIn() {
        router.push('/login');
    }

    function goToSignUp() {
        router.push('/signup');
    }
</script>