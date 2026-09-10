<!-- ARCHIVED -->
<template>
  <input v-model="email" />
  <input v-model="password"/>
  <button>Sign Up</button>
</template>

<script setup>
    import { ref } from "vue";
    import axios from "axios";

    const email = ref("");
    const password = ref("");

    function handleSubmit(){

    }
</script>
