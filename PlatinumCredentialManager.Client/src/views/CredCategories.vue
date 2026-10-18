<template>
  <Header></Header>
  <div>Credential Categories</div>
  <div v-if="isLoading">Loading Credential Categories...</div>
  <div v-else-if="!isLoading && !successfullyLoaded">
    {{ errorMessage }}
  </div>
  <div v-else>
    <button @click="createModal('create-category')">Create Category</button>
    <!-- Only shows category name -->
    <PaginationGrid 
      :rows="allCategories" 
      :location="'allCategories'" 
      @rerun="categoryLoaderHelper"
    />
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

import { useModal } from "@/composables/useModal";
import { serviceHandler } from "@/services/serviceHandler";
import { useToast } from "vue-toastification";

const { showModal, action, createModal, closeModal } = useModal();

const allCategories = ref(null);
const isLoading = ref(true);
const successfullyLoaded = ref(true);
const errorMessage = ref("");

async function categoryLoaderHelper() {
  try {
    const { valid, data } = await serviceHandler('get-user');
    
    if (valid){
      allCategories.value = data.categories;
    } else {
      errorMessage.value = `${data.title}\n${data.detail}`
      useToast().error(errorMessage.value);
    }
  } catch (error) {
    console.error("Failed to retrive credential categories", error);
    successfullyLoaded.value = false;
  } finally {
    isLoading.value = false;
  }
}

onMounted(categoryLoaderHelper);
</script>
