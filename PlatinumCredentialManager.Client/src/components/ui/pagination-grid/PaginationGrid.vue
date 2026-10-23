<template>
  <div v-if="location !== 'credential'">
    <label for="sortFilter">Sort Options:</label>
    <select name="sortFilter" v-model="sortOption" :id="'sortFilter'">
      <option
        v-if="location === 'allCredentials' || location === 'searchup'"
        value="azAscendingService"
      >
        Service Name (Ascending Order)
      </option>
      <option
        v-if="location === 'allCredentials' || location === 'searchup'"
        value="azDescendingService"
      >
        Service Name (Descending Order)
      </option>
      <option
        v-if="location === 'allCategories' || location === 'searchup'"
        value="azAscendingCategory"
      >
        Category Name (Ascending Order)
      </option>
      <option
        v-if="location === 'allCategories' || location === 'searchup'"
        value="azDescendingCategory"
      >
        Category Name (Descending Order)
      </option>
      <option
        v-if="location === 'allCredentials' || location === 'searchup'"
        value="dateCreatedAscending"
      >
        Date Created (Ascending Order)
      </option>
      <option
        v-if="location === 'allCredentials' || location === 'searchup'"
        value="dateCreatedDescending"
      >
        Date Created (Descending Order)
      </option>
      <option
        v-if="location === 'allCredentials' || location === 'searchup'"
        value="dateUpdatedAscending"
      >
        Date Updated (Ascending Order)
      </option>
      <option
        v-if="location === 'allCredentials' || location === 'searchup'"
        value="dateUpdatedDescending"
      >
        Date Updated (Descending Order)
      </option>
      <option v-if="location === 'searchup'" value="byMatchCredential">
        By Match (Credential)
      </option>
    </select>

    <div v-if="location === 'allCategories'">
      <label for="withinCategorySortOption">Sorting Within Category: </label>
      <select name="withinCategorySortOption" :id="'withinCategorySort'" v-model="withinCategorySortOption">
        <option
          value="azAscendingService"
        >
          Service Name (Ascending Order)
        </option>
        <option
          value="azDescendingService"
        >
          Service Name (Descending Order)
        </option>
        <option
          value="dateCreatedAscending"
        >
          Date Created (Ascending Order)
        </option>
        <option
          value="dateCreatedDescending"
        >
          Date Created (Descending Order)
        </option>
        <option
          value="dateUpdatedAscending"
        >
          Date Updated (Ascending Order)
        </option>
        <option
          value="dateUpdatedDescending"
        >
          Date Updated (Descending Order)
        </option>
      </select>
    </div>
  </div>

  <div>
    <button v-if="location === 'allCategories' || location === 'allCredentials'" @click="createModal(location)">Import CSV</button>
    <button @click="exportCSVDecider(location, rows, sortOption, withinCategorySortOption, (location === 'searchup' ? parsePayload(params): null))">Export CSV</button>
  </div>

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

  <PaginationGridPageTracker 
    v-if="location !== 'credential'"
    v-model:pageNumber="pageNumber"
    v-model:inputPageNumber="inputPageNumber"
    v-model:currentData="currentData"
    :allData="allData"
  />

  <ActionFormModal
    v-if="location === 'allCategories' || location === 'allCredentials'"
    :show="showModal"
    :action="action"
    @close="closeModal"
    @rerun="loaderHelper"
  />
</template>

<script setup>
import { ref, onMounted, watch, inject } from "vue";
import { useRouter, useRoute } from "vue-router";

import { paginateData } from "@/utils/pagination";
import { categoryPath, credentialPath } from "@/router/routes.js";
import { parsePayload } from "@/utils/parsePayload.js";
import { sortDecider } from "@/utils/sort";
import { exportCSVDecider } from "@/csv/exportCSV.js";
import { useModal } from "@/composables/useModal.js";
import ActionFormModal from "@/components/ui/action-form-modal/ActionFormModal.vue";
import PaginationGridPageTracker from "./PaginationGridPageTracker.vue";

const { showModal, action, createModal, closeModal } = useModal();

const { rows, location, params, credentialMatch } = defineProps({
  rows: Array,
  location: String,
  params: Object,
  credentialMatch: String,
});

const router = useRouter();
const route = useRoute();

const allData = ref([]);
const currentData = ref([]);
const userId = ref(Number(route.params.user_id));
const categoryId = ref(null);
const pageNumber = ref(1);
const inputPageNumber = ref(pageNumber.value);
const sortOption = ref("");
const withinCategorySortOption = ref("");

const emit = defineEmits(["close", "rerun"]);

// for credential
const showPassword = ref(false);

watch(sortOption, () => changeData());
watch(() => rows, () => changeData()); // for import csv if new things are added

function changeData(){
  if (location !== 'credential'){
    allData.value = paginateData(
      sortDecider(sortOption.value, null, rows, credentialMatch),
    );
    pageNumber.value = 1;
    currentData.value = allData.value[pageNumber.value - 1];
    inputPageNumber.value = pageNumber.value;
  }
}

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

// for importing a csv
function loaderHelper(){
  emit('rerun');
}

// When in credentials of a category
function redirectToCredentialsOfCategory(categoryIdSpecific) {
  router.push(categoryPath(categoryIdSpecific, userId.value));
}

// First, when we reach onMounted => we have the data => we need to first paginate it
onMounted(() => {
  categoryId.value = inject("categoryId");
  sortOption.value =
    location === "allCategories" ? "azAscendingCategory" : (location !== 'credential' ? "azAscendingService" : null);
  withinCategorySortOption.value = location === "allCategories" ? "azAscendingService" : "";
});
</script>
