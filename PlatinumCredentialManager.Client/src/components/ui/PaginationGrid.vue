<template>
  <div v-if="action !== 'credential'">
    <label for="sortFilter">Sort Options:</label>
    <select name="sortFilter" v-model="sortOption" :id="sortFilter">
      <option
        v-if="action === 'allCredentials' || action === 'searchup'"
        value="azAscendingService"
      >
        Service Name (Ascending Order)
      </option>
      <option
        v-if="action === 'allCredentials' || action === 'searchup'"
        value="azDescendingService"
      >
        Service Name (Descending Order)
      </option>
      <option
        v-if="action === 'allCategories' || action === 'searchup'"
        value="azAscendingCategory"
      >
        Category Name (Ascending Order)
      </option>
      <option
        v-if="action === 'allCategories' || action === 'searchup'"
        value="azDescendingCategory"
      >
        Category Name (Descending Order)
      </option>
      <option
        v-if="action === 'allCredentials' || action === 'searchup'"
        value="dateCreatedAscending"
      >
        Date Created (Ascending Order)
      </option>
      <option
        v-if="action === 'allCredentials' || action === 'searchup'"
        value="dateCreatedDescending"
      >
        Date Created (Descending Order)
      </option>
      <option
        v-if="action === 'allCredentials' || action === 'searchup'"
        value="dateUpdatedAscending"
      >
        Date Updated (Ascending Order)
      </option>
      <option
        v-if="action === 'allCredentials' || action === 'searchup'"
        value="dateUpdatedDescending"
      >
        Date Updated (Descending Order)
      </option>
      <option v-if="action === 'searchup'" value="byMatchCredential">
        By Match (Credential)
      </option>
    </select>

    <div v-if="action === 'allCategories'">
      <label for="withinCategorySortOption">Sorting Within Category: </label>
      <select name="withinCategorySortOption" :id="withinCategorySort" v-model="withinCategorySortOption">
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
    <button @click="">Import CSV</button>
    <button @click="exportCSVDecider(action, currentData, sortOption, withinCategorySortOption, (action === 'searchup' ? parsePayload(params): null))">Export CSV</button>
  </div>

  <div v-if="action === 'allCategories'">
    <div
      v-for="category in currentData"
      :key="category.id"
      @click="redirectToCredentialsOfCategory(category.id)"
    >
      {{ category.categoryName }}
    </div>
  </div>

  <div v-else-if="action === 'searchup'">
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

  <div v-else-if="action === 'allCredentials'">
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

  <div v-else-if="action === 'credential'">
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

  <div v-if="action !== 'credential'">
    <button :disabled="pageNumber === 1" @click="checkDecrement">«</button>
    <input
      v-model="inputPageNumber"
      type="number"
      min="1"
      :max="allData.length"
      @keyup.enter="goToPage"
    />
    <button :disabled="pageNumber === allData.length" @click="checkIncrement">
      »
    </button>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from "vue";
import { useRouter, useRoute } from "vue-router";

import { paginateData } from "@/utils/pagination";
import { categoryPath, credentialPath } from "@/utils/routes.js";
import { parsePayload } from "@/utils/parsePayload.js";
import { sortDecider } from "@/utils/sort";
import { exportCSVDecider } from "@/utils/exportCSV";

const { categoryId, rows, action, params, credentialMatch } = defineProps({
  categoryId: Number,
  rows: Array,
  action: String,
  params: Object,
  credentialMatch: String,
});

const allData = ref([]);
const currentData = ref([]);
const pageNumber = ref(1);
const inputPageNumber = ref(pageNumber.value);
const sortOption = ref("");
const withinCategorySortOption = ref("");

const router = useRouter();
const route = useRoute();

// for credential
const showPassword = ref(false);

watch(sortOption, () => changeData());

function changeData(){
  allData.value = paginateData(
    sortDecider(sortOption.value, null, rows, credentialMatch),
  );
  pageNumber.value = 1;
  currentData.value = allData.value[pageNumber.value - 1];
  inputPageNumber.value = pageNumber.value;
}


function goToPage() {
  if (!Number.isInteger(inputPageNumber.value)) {
    inputPageNumber.value = Math.round(inputPageNumber.value);
  }

  if (inputPageNumber.value > allData.value.length) {
    inputPageNumber.value = allData.value.length;
    pageNumber.value = inputPageNumber.value;
    currentData.value = allData.value[pageNumber.value - 1];
    return;
  } else if (inputPageNumber.value < 1) {
    inputPageNumber.value = 1;
    pageNumber.value = inputPageNumber.value;
    currentData.value = allData.value[pageNumber.value - 1];
    return;
  }
  currentData.value = allData.value[inputPageNumber.value];
  pageNumber.value = inputPageNumber.value;
}

function checkIncrement() {
  if (pageNumber.value !== allData.value.length) {
    pageNumber.value += 1;
    inputPageNumber.value = pageNumber.value;
    currentData.value = allData.value[pageNumber.value - 1];
  }
}

function checkDecrement() {
  if (pageNumber.value !== 1) {
    pageNumber.value -= 1;
    inputPageNumber.value = pageNumber.value;
    currentData.value = allData.value[pageNumber.value - 1];
  }
}

// When in category
function redirectCredential(credentialId) {
  router.push(credentialPath(credentialId, categoryId, route.params.user_id));
}

// When in searchup of categories
function redirectCredentialQuery(credential) {
  router.push({
    path: credentialPath(
      credential.credentialId,
      credential.categoryId,
      route.params.user_id,
    ),
    query: parsePayload(params),
  });
}

// When in credentials of a category
function redirectToCredentialsOfCategory(categoryId) {
  router.push(categoryPath(categoryId, route.params.user_id));
}

// First, when we reach onMounted => we have the data => we need to first paginate it
onMounted(() => {
  sortOption.value =
    action === "allCategories" ? "azAscendingCategory" : (action !== 'credential' ? "azAscendingService" : null);
  withinCategorySortOption.value = action === "allCategories" ? "azAscendingService" : "";
});
</script>
