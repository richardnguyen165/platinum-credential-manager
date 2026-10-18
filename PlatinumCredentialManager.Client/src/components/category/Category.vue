<template>
  <Header></Header>
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
    <Form
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
import Header from "../layout/Header.vue";
import Form from "../ui/Form.vue";
import PaginationGrid from "../ui/PaginationGrid.vue";

import { onMounted, ref, reactive } from "vue";
import { useRouter, useRoute } from "vue-router";
import { categoriesPath } from "@/utils/routes.js";
import { useModal } from "@/composables/useModal.js";
import { serviceHandler } from "@/services/serviceHandler.js";
import { useToast } from "vue-toastification";

const { showModal, action, createModal, closeModal } = useModal();

const isMiscallaneous = ref(null);
const allCredentials = ref(null);
const isLoading = ref(true);
const successfullyLoaded = ref(true);
const errorMessage = ref("");
const router = useRouter();
const route = useRoute();
const currentEntries = reactive({});

function redirectBackToAllCategories() {
  router.push(categoriesPath(route.params.user_id));
}

const { categoryId } = defineProps({
  categoryId: Number,
});

async function loaderHelper() {
  try {
    const payload = {
      "id": categoryId
    }

    const { valid, data } = await serviceHandler('get-category', payload);
    console.log(data);
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
