<template>
  <PaginationGridSortFilter 
    v-if="grid.location !== 'credential'"
    v-model:sortOption="sortOption"
    v-model:withinCategorySortOption="withinCategorySortOption"
    :location="grid.location"
  />

  <div>
    <button v-if="grid.location === 'allCategories' || grid.location === 'allCredentials'" @click="createModal(grid.location)">Import CSV</button>
    <button @click="exportCSVDecider(location, rows, sortOption, withinCategorySortOption, (location === 'searchup' ? parsePayload(params): null))">Export CSV</button>
  </div>
  
  <PaginationGridDataDisplay 
    :grid="grid"
    :currentData="currentData"
  />

  <PaginationGridPageTracker 
    v-if="grid.location !== 'credential'"
    v-model:pageNumber="pageNumber"
    v-model:inputPageNumber="inputPageNumber"
    v-model:currentData="currentData"
    :allData="allData"
  />

  <ActionFormModal
    v-if="grid.location === 'allCategories' || grid.location === 'allCredentials'"
    :show="showModal"
    :action="action"
    @close="closeModal"
    @rerun="loaderHelper"
  />
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from "vue";

import { paginateData } from "@/utils/pagination.js";
import { parsePayload } from "@/utils/parsePayload.js";
import { sortDecider } from "@/utils/sort.js";
import { exportCSVDecider } from "@/csv/exportCSV.js";
import { useModal } from "@/composables/useModal.js";
import ActionFormModal from "@/components/ui/action-form-modal/ActionFormModal.vue";
import PaginationGridPageTracker from "./PaginationGridPageTracker.vue";
import PaginationGridDataDisplay from "./PaginationGridDataDisplay.vue";
import PaginationGridSortFilter from "./PaginationGridSortFilter.vue";
import type { GridData } from "@/types/gridType.js";
import type { DataRow, SortAction } from "@/types/sortType.js";

const { showModal, action, createModal, closeModal } = useModal();

const { grid } = defineProps<{
  grid: GridData
}>();

const allData = ref<DataRow[][]>([]);
const currentData = ref<DataRow[]>([]);
const pageNumber = ref(1);
const inputPageNumber = ref(pageNumber.value);
const sortOption = ref<SortAction | null>("");
const withinCategorySortOption = ref("");

const emit = defineEmits(["close", "rerun"]);

watch(sortOption, () => changeData());
if (grid.location === 'allCategories' || grid.location === 'allCredentials') watch(() => grid.rows, () => changeData()); // for import csv if new things are added

function changeData(){
  if (sortOption.value === null) return;
  if (grid.location !== 'credential'){
    let result;
    if (grid.location === 'searchup') result = sortDecider(sortOption.value, null, grid.rows, grid.credentialMatch);
    else result = sortDecider(sortOption.value, null, grid.rows, "");
    allData.value = paginateData(result);
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
    grid.location === "allCategories" ? "azAscendingCategory" : (grid.location !== 'credential' ? "azAscendingService" : null);
  withinCategorySortOption.value = grid.location === "allCategories" ? "azAscendingService" : "";
});
</script>
