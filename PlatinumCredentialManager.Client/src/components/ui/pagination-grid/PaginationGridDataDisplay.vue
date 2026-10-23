<template>
  <div v-if="location === 'allCategories'">
    <div
      v-for="category in currentData"
      :key="category.id"
      @click="redirectToCredentialsOfCategory(category.id)"
    >
      {{ category.categoryName }}
    </div>
  </div>

  <div v-else-if="location === 'searchup'">
    <div
      @click="redirectCredentialQuery(credential)"
      v-for="credential in currentData"
      :key="credential.credentialId"
    >
      <div>Service Name: {{ credential.serviceName }}</div>
      <div>Category Name: {{ credential.categoryName }}</div>
      <div>Date Created: {{ credential.dateCreated }}</div>
      <div>Date Last Updated: {{ credential.dateLastUpdated }}</div>
    </div>
  </div>

  <div v-else-if="location === 'allCredentials'">
    <div
      @click="redirectCredential(credential.id)"
      v-for="credential in currentData"
      :key="credential.id"
    >
      <div>Service Name: {{ credential.serviceName }}</div>
      <div>Date Created: {{ credential.dateCreated }}</div>
      <div>Date Last Updated: {{ credential.dateLastUpdated }}</div>
    </div>
  </div>

  <div v-else-if="location === 'credential'">
    <div>Category Name: {{ rows.CategoryName }}</div>
    <p>Service Name: {{ rows.ServiceName }}</p>
    <p>Username: {{ rows.Username }}</p>
    <p>
      Password:
      <input
        :type="showPassword ? 'text' : 'password'"
        :id="password"
        v-model="rows.Password"
        readonly
      />
      <button @click="showPassword = !showPassword">
        {{ showPassword ? "Hide" : "Show" }} Password
      </button>
    </p>
    <p>Date Created: {{ rows.DateCreated }}</p>
    <p>Date Last Updated: {{ rows.DateLastUpdated }}</p>
  </div>
</template>


<script setup>
import { categoryPath, credentialPath } from '@/router/routes';
import { parsePayload } from '@/utils/parsePayload';
import { ref, inject } from 'vue';
import { useRoute, useRouter } from 'vue-router';

// for credential

const { currentData, rows, location, params } = defineProps({
    currentData: Array,
    location: String,
    rows: Object,
    params: Object
})

const router = useRouter();
const route = useRoute();

const userId = ref(Number(route.params.user_id));
const categoryId = ref(inject("categoryId"));
const showPassword = ref(false);
    
// When in category
function redirectCredential(credentialId) {
  router.push(credentialPath(credentialId, categoryId.value, userId.value));
}

// When in searchup of categories
function redirectCredentialQuery(credential) {
  router.push({
    path: credentialPath(
      credential.credentialId,
      credential.categoryId,
      userId.value,
    ),
    query: parsePayload(params),
  });
}

// When in credentials of a category
function redirectToCredentialsOfCategory(categoryIdSpecific) {
  router.push(categoryPath(categoryIdSpecific, userId.value));
}
</script>