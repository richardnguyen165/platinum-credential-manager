<template>
  <input v-model="email" />
  <input v-model="password"/>
  <button>Log In</button>
</template>

<script setup>
    import { ref } from "vue";
    import axios from "axios";
    import { authState, login, logout, refresh } from "@/composables/useAuth"

    const email = ref("");
    const password = ref("");

    function handleSubmit(){
        login
    }
</script>