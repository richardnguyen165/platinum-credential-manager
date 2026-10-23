<template>
  <PaginationGridSortFilter 
    v-if="location !== 'credential'"
    v-model:sortOption="sortOption"
    v-model:withinCategorySortOption="withinCategorySortOption"
    :location="location"
  />

  <div>
    <button v-if="location === 'allCategories' || location === 'allCredentials'" @click="createModal(location)">Import CSV</button>
    <button @click="exportCSVDecider(location, rows, sortOption, withinCategorySortOption, (location === 'searchup' ? parsePayload(params): null))">Export CSV</button>
  </div>
  
  <PaginationGridDataDisplay 
    :rows="rows"
    :currentData="currentData"
    :location="location"
    :params="params"
  />

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
import { ref, onMounted, watch } from "vue";

import { paginateData } from "@/utils/pagination";
import { parsePayload } from "@/utils/parsePayload.js";
import { sortDecider } from "@/utils/sort";
import { exportCSVDecider } from "@/csv/exportCSV.js";
import { useModal } from "@/composables/useModal.js";
import ActionFormModal from "@/components/ui/action-form-modal/ActionFormModal.vue";
import PaginationGridPageTracker from "./PaginationGridPageTracker.vue";
import PaginationGridDataDisplay from "./PaginationGridDataDisplay.vue";
import PaginationGridSortFilter from "./PaginationGridSortFilter.vue";

const { showModal, action, createModal, closeModal } = useModal();

const { rows, location, params, credentialMatch } = defineProps({
  rows: Array,
  location: String,
  params: Object,
  credentialMatch: String,
});

const allData = ref([]);
const currentData = ref([]);
const pageNumber = ref(1);
const inputPageNumber = ref(pageNumber.value);
const sortOption = ref("");
const withinCategorySortOption = ref("");

const emit = defineEmits(["close", "rerun"]);

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

// for importing a csv
function loaderHelper(){
  emit('rerun');
}

// First, when we reach onMounted => we have the data => we need to first paginate it
onMounted(() => {
  sortOption.value =
    location === "allCategories" ? "azAscendingCategory" : (location !== 'credential' ? "azAscendingService" : null);
  withinCategorySortOption.value = location === "allCategories" ? "azAscendingService" : "";
});
</script>
