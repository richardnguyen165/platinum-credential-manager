<template>
  <Header></Header>
  <div>Credential Categories</div>
  <div v-if="isLoading">Loading Credential Categories...</div>
  <div v-else-if="!isLoading && !successfullyLoaded">
    Error: Loading credential categories failed!
  </div>
  <div v-else>
    <button @click="createModal('create-category')">Create Category</button>
    <button>Import Category and Credentials</button>
    <button>Export Category and Credentials</button>
    <!-- Only shows category name -->
    <PaginationGrid :rows="allCategories" :action="'allCategories'" />
    <Form
      :show="showModal"
      :action="action"
      @close="closeModal"
      @rerun="categoryLoaderHelper"
    />
  </div>
</template>

<script setup>
import { onMounted, ref } from "vue";

import Form from "@/components/ui/Form.vue";
import Header from "@/components/layout/Header.vue";
import PaginationGrid from "@/components/ui/PaginationGrid.vue";

import { getUser } from "@/services/userService";
import { useModal } from "@/composables/useModal";

const { showModal, action, createModal, closeModal } = useModal();

const allCategories = ref(null);
const isLoading = ref(true);
const successfullyLoaded = ref(true);

async function categoryLoaderHelper() {
  try {
    const user = await getUser();
    allCategories.value = user.categories;
  } catch (error) {
    console.error("Failed to retrive credential categories", error);
    successfullyLoaded.value = false;
  } finally {
    isLoading.value = false;
  }
}

onMounted(categoryLoaderHelper);
</script>
