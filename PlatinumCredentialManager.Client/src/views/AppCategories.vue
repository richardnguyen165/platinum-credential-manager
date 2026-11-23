<template>
  <AppHeader></AppHeader>
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
    <ActionFormModal
      :show="showModal"
      :action="action"
      @close="closeModal"
      @rerun="categoryLoaderHelper"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, provide, type Ref } from "vue";

import ActionFormModal from "@/components/ui//action-form-modal/ActionFormModal.vue";
import AppHeader from "@/components/layout/AppHeader.vue";
import PaginationGrid from "@/components/ui/pagination-grid/PaginationGrid.vue";

import { useModal } from "@/composables/useModal.js";
import { serviceHandler } from "@/services/serviceHandler.js";
import { useToast } from "vue-toastification";
import type { ServiceCall } from "@/types/requestType.js";
import type { CategorySummary } from "../types/responseType.js";

const { showModal, action, createModal, closeModal } = useModal();

const allCategories: Ref<CategorySummary[]> = ref([]);
const isLoading = ref(true);
const successfullyLoaded = ref(true);
const errorMessage = ref("");

provide('categoryId', null);
provide('credentialId', null);

async function categoryLoaderHelper() {
  try {
    const payload: ServiceCall = {
      action: 'get-user'
    }
    const { valid, data: extractedData } = await serviceHandler(payload);
    
    if (valid){
      allCategories.value = extractedData.categories;
    } else {
      errorMessage.value = `${extractedData?.title}\n${extractedData?.detail}`
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
