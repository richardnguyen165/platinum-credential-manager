<template>
  <div v-if="grid.location === 'allCategories'">
    <div
      v-for="category in grid.rows"
      :key="category.id"
      @click="redirectToCredentialsOfCategory(String(category.id))"
    >
      {{ category.categoryName }}
    </div>
  </div>

  <div v-else-if="grid.location === 'searchup'">
    <div
      @click="redirectCredentialQuery(credential)"
      v-for="credential in grid.rows"
      :key="credential.credentialId"
    >
      <div>Service Name: {{ credential.serviceName }}</div>
      <div>Category Name: {{ credential.categoryName }}</div>
      <div>Date Created: {{ credential.dateCreated }}</div>
      <div>Date Last Updated: {{ credential.dateLastUpdated }}</div>
    </div>
  </div>

  <div v-else-if="grid.location === 'allCredentials'">
    <div
      @click="redirectCredential(String(credential.id))"
      v-for="credential in grid.rows"
      :key="credential.id"
    >
      <div>Service Name: {{ credential.serviceName }}</div>
      <div>Date Created: {{ credential.dateCreated }}</div>
      <div>Date Last Updated: {{ credential.dateLastUpdated }}</div>
    </div>
  </div>

  <div v-else-if="grid.location === 'credential'">
    <div>Category Name: {{ credential?.CategoryName }}</div>
    <p>Service Name: {{ credential?.ServiceName }}</p>
    <p>Username: {{ credential?.Username }}</p>
    <p>
      Password:
      <input
        :type="showPassword ? 'text' : 'password'"
        :id="'password'"
        v-model="credential?.Password"
        readonly
      />
      <button @click="showPassword = !showPassword">
        {{ showPassword ? "Hide" : "Show" }} Password
      </button>
    </p>
    <p>Date Created: {{ credential?.DateCreated }}</p>
    <p>Date Last Updated: {{ credential?.DateLastUpdated }}</p>
  </div>
</template>


<script setup lang="ts">
import { categoryPath, credentialPath } from '@/router/routes.js';
import type { CurrentEntries } from '@/types/formType.js';
import type { GridData } from '@/types/gridType.js';
import { categoryIdKey } from '@/types/injectionKeys.js';
import type { CredentialSearchResult } from '@/types/responseType.js';
import { parsePayload } from '@/utils/parsePayload.js';
import { ref, inject, computed, type ComputedRef } from 'vue';
import { useRoute, useRouter } from 'vue-router';

// for credential

const { grid } = defineProps<{
  grid: GridData
}>();

const credential: ComputedRef<CurrentEntries | undefined> = computed(() =>
  grid.location === 'credential' && !Array.isArray(grid.rows) ? grid.rows : undefined
);

const router = useRouter();
const route = useRoute();

const userId = ref(Number(route.params.user_id));
const showPassword = ref(false);

const injectedCategoryId = inject(categoryIdKey);
if (injectedCategoryId == null) {
  throw new Error("categoryId was not provided");
}
const categoryId: number = injectedCategoryId;

// When in category
async function redirectCredential(credentialId: string) {
  await router.push(credentialPath(credentialId, categoryId, userId.value));
}

// When in searchup of categories
async function redirectCredentialQuery(credential: CredentialSearchResult) {
  if (grid.location === 'searchup'){
    if (grid.params == null) return;
    await router.push({
      path: credentialPath(
        credential.credentialId,
        credential.categoryId,
        userId.value,
      ),
      query: parsePayload(grid.params),
    });
  }
}

// When in credentials of a category
async function redirectToCredentialsOfCategory(categoryIdSpecific: string) {
  await router.push(categoryPath(categoryIdSpecific, userId.value));
}
</script>