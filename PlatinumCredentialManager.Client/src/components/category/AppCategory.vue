<template>
  <AppHeader></AppHeader>
  <div v-if="isLoading">Loading Credentials...</div>
  <div v-else-if="!isLoading && !successfullyLoaded">
    {{ errorMessage }}
  </div>
  <div v-else>
    <p>Category Name: {{ currentEntries.CategoryName }}</p>
    <button @click="redirectBackToAllCategories"> Go Back to All Categories </button>
    <button v-if="!isMiscallaneous" @click="createModal('edit-category')"> Edit Category </button>
    <button v-if="!isMiscallaneous" @click="createModal('delete-category')"> Delete Category </button>
    <button @click="createModal('create-credential')">Create Credential</button>
    <PaginationGrid
      :categoryId="categoryId"
      :rows="allCredentials"
      :location="'allCredentials'"
      @rerun="loaderHelper"
    />
    <ActionFormModal
      :show="showModal"
      :categoryId="categoryId"
      :action="action"
      :currentEntries="currentEntries"
      @close="closeModal"
      @rerun="loaderHelper"
    />
  </div>
</template>

<script setup>
import { onMounted, ref, reactive, provide } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useToast } from "vue-toastification";

import AppHeader from "@/components/layout/AppHeader.vue";
import ActionFormModal from "@/components/ui//action-form-modal/ActionFormModal.vue";
import PaginationGrid from "@/components/ui/pagination-grid/PaginationGrid.vue";
import { categoriesPath } from "@/router/routes.js";
import { useModal } from "@/composables/useModal.js";
import { serviceHandler } from "@/services/serviceHandler.js";

const { showModal, action, createModal, closeModal } = useModal();

const router = useRouter();
const route = useRoute();

const isMiscallaneous = ref(null);
const allCredentials = ref(null);
const isLoading = ref(true);
const successfullyLoaded = ref(true);
const errorMessage = ref("");
const categoryId = ref(Number(route.params.category_id));
const userId = ref(Number(route.params.user_id));
const currentEntries = reactive({});

provide('categoryId', Number(route.params.category_id));
provide('credentialId', null);

function redirectBackToAllCategories() {
  router.push(categoriesPath(userId.value));
}

async function loaderHelper() {
  try {
    const payload = {
      "id": categoryId.value
    }

    const { valid, data } = await serviceHandler('get-category', payload);
    if (valid) {
      allCredentials.value = data.credentials;
      currentEntries.CategoryName = data.categoryName;
      isMiscallaneous.value = currentEntries.CategoryName === "Miscallaneous";
    } else {
      errorMessage.value=`${data.title}\n${data.detail}`
      useToast().error(errorMessage.value);
    }
  } catch (error) {
    console.error("Failed to retrive credentals", error);
    successfullyLoaded.value = false;
  } finally {
    isLoading.value = false;
  }
}

onMounted(loaderHelper);
</script>
