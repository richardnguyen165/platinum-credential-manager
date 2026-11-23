<template>
  <AppHeader></AppHeader>
  <div v-if="isLoading">Loading Credential...</div>
  <div v-else-if="!isLoading && !successfullyLoaded">
    {{ errorMessage }}
  </div>
  <div v-else>
    <button @click="goBackPrevious">Go Back</button>
    <button @click="createModal('delete-credential')">Delete Credential</button>
    <button @click="createModal('edit-credential')">Edit Credential</button>
    <PaginationGrid
      :rows="currentEntries"
      :location="'credential'"
      @rerun="credentialLoaderHelper"
    />
    <ActionFormModal
      :show="showModal"
      :action="action"
      :currentEntries="currentEntries"
      :query="query"
      @close="closeModal"
      @rerun="credentialLoaderHelper"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive, watch, provide, type Reactive } from "vue";
import { useRouter, useRoute } from "vue-router";

import ActionFormModal from "@/components/ui//action-form-modal/ActionFormModal.vue";
import AppHeader from "@/components/layout/AppHeader.vue"
import PaginationGrid from "@/components/ui/pagination-grid/PaginationGrid.vue";

import { useToast } from 'vue-toastification'

import { categoryPath, searchUpPath } from "@/router/routes.js";
import { useModal } from "@/composables/useModal.js";
import { serviceHandler } from "@/services/serviceHandler.js";
import type { ServiceCall } from "@/types/requestType.js";
import type { CurrentEntries } from "@/types/formType.js";

const { showModal, action, createModal, closeModal } = useModal();

const { query } = defineProps({
  query: { type: Object, required: true },
});

const router = useRouter();
const route = useRoute();

const isLoading = ref(true);
const successfullyLoaded = ref(true);
const errorMessage = ref("");
const userId = ref(Number(route.params.user_id));
const categoryId = ref(Number(route.params.category_id));
const credentialId = ref(Number(route.params.cred_id));
const currentEntries: Reactive<CurrentEntries> = reactive({});

provide("categoryId", categoryId.value);
provide("credentialId", credentialId.value);

watch(() => categoryId.value, credentialLoaderHelper);

function goBackPrevious() {
  if (Object.keys(query).length > 0) {
    router.push({ path: searchUpPath(userId.value), query });
  } else {
    router.push(categoryPath(categoryId.value, userId.value));
  }
}

async function credentialLoaderHelper() {
  try {
    const payload: ServiceCall = {
      action: 'get-credential',
      payload: {
        "id": credentialId.value
      }
    }
    

    const { valid, data: extractedData } = await serviceHandler(payload);
    
    if (valid){
      currentEntries.CategoryName = extractedData.categoryName;
      currentEntries.ServiceName = extractedData.serviceName;
      currentEntries.Username = extractedData.username;
      currentEntries.Password = extractedData.password;
      currentEntries.DateCreated = extractedData.dateCreated;
      currentEntries.DateLastUpdated = extractedData.dateLastUpdated;
    } else {
      errorMessage.value=`${extractedData?.title}: ${extractedData?.detail}`
      useToast().error(errorMessage.value);
    }
  } catch (error) {
    console.error("Failed to retrive credential", error);
    successfullyLoaded.value = false;
  } finally {
    isLoading.value = false;
  }
}

onMounted(credentialLoaderHelper);
</script>
