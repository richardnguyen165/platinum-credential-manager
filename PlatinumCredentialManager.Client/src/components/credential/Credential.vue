<template>
  <Header></Header>
  <div v-if="isLoading">Loading Credential...</div>
  <div v-else-if="!isLoading && !successfullyLoaded">
    Error: Loading credential failed!
  </div>
  <div v-else>
    <button @click="goBackPrevious">Go Back</button>
    <button @click="createModal('delete-credential')">Delete Credential</button>
    <button @click="createModal('edit-credential')">Edit Credential</button>
    <PaginationGrid
      :rows="currentEntries"
      :action="'credential'"
      :categoryId="categoryId"
    />
    <Form
      :show="showModal"
      :categoryId="categoryId"
      :credentialId="credId"
      :action="action"
      :currentEntries="currentEntries"
      @close="closeModal"
      @rerun="credentialLoaderHelper"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, reactive } from "vue";
import { useRouter, useRoute } from "vue-router";

import Form from "../ui/Form.vue";
import Header from "../layout/Header.vue";
import PaginationGrid from "../ui/PaginationGrid.vue";

import { getCredential } from "@/services/credentialService.js";
import { categoryPath, searchUpPath } from "@/utils/routes.js";
import { useModal } from "@/composables/useModal.js";

const { showModal, action, createModal, closeModal } = useModal();

const { categoryId, credId, query } = defineProps({
  categoryId: Number,
  credId: Number,
  query: Object,
});

const router = useRouter();
const route = useRoute();

const isLoading = ref(true);
const successfullyLoaded = ref(true);
const currentEntries = reactive({});

function goBackPrevious() {
  if (Object.keys(query).length > 0) {
    router.push({ path: searchUpPath(route.params.user_id), query });
  } else {
    router.push(categoryPath(categoryId, route.params.user_id));
  }
}

async function credentialLoaderHelper() {
  try {
    const data = await getCredential(credId);

    currentEntries.ServiceName = data.serviceName;
    currentEntries.Username = data.username;
    currentEntries.Password = data.password;
    currentEntries.DateCreated = data.dateCreated;
    currentEntries.DateLastUpdated = data.dateLastUpdated;
  } catch (error) {
    console.error("Failed to retrive credential", error);
    successfullyLoaded.value = false;
  } finally {
    isLoading.value = false;
  }
}

onMounted(credentialLoaderHelper);
</script>
