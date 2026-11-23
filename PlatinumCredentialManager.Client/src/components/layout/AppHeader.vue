<template>
  <div>
    <button @click="executeRedirect('home')">Home</button>

    <button @click="executeRedirect('categories')">
      Credential Categories
    </button>

    <button @click="executeRedirect('search')">Search</button>

    <button @click="executeRedirect('logout')">Log-Out</button>
  </div>
</template>

<script setup lang="ts">
import useAuth from "@/composables/useAuth.js";

import { homePath, categoriesPath, searchUpPath } from "@/router/routes.js";
import type { RedirectParam } from "@/types/headerType.js";
import { useRoute, useRouter } from "vue-router";

const route = useRoute();
const router = useRouter();

const { logout } = useAuth();

function executeRedirect(redirectParam: RedirectParam) {
  let newRoute = null;

  const user_id = Array.isArray(route.params.user_id) ? route.params.user_id[0] : route.params.user_id;

  switch (redirectParam) {
    case "categories":
      newRoute = categoriesPath(user_id);
      break;
    case "search":
      newRoute = searchUpPath(user_id);
      break;
    default:
      newRoute = homePath();
  }

  if (redirectParam === "logout") {
    logout();
  } else router.push(newRoute);
}
</script>
