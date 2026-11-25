<template>
  <PaginationGridSortFilter 
    v-if="grid.location !== 'credential'"
    v-model:sortOption="sortOption"
    v-model:withinCategorySortOption="withinCategorySortOption"
    :location="grid.location"
  />

  <div>
    <button v-if="grid.location === 'allCategories' || grid.location === 'allCredentials'" @click="createModal(grid.location)">Import CSV</button>
    <button @click="exportCSVDecider(grid, sortOption, withinCategorySortOption)">Export CSV</button>
  </div>
  
  <PaginationGridDataDisplay 
    :grid="modifiedGrid"
  />

  <PaginationGridPageTracker 
    v-if="grid.location !== 'credential'"
    v-model:pageNumber="pageNumber"
    :pageTotal="allPaginatedAndSortedRows?.length ?? 0"
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
import { ref, computed } from "vue";

import { exportCSVDecider } from "@/csv/exportCSV.js";
import { useModal } from "@/composables/useModal.js";
import ActionFormModal from "@/components/ui/action-form-modal/ActionFormModal.vue";
import PaginationGridPageTracker from "./PaginationGridPageTracker.vue";
import PaginationGridDataDisplay from "./PaginationGridDataDisplay.vue";
import PaginationGridSortFilter from "./PaginationGridSortFilter.vue";
import type { GridData } from "@/types/gridType.js";
import type { SortAction } from "@/types/sortType.js";
import { sortDecider } from "@/utils/sort.js";
import { paginateData } from "@/utils/pagination.js";

const { showModal, action, createModal, closeModal } = useModal();

const { grid } = defineProps<{
  grid: GridData
}>();

const pageNumber = ref(1);
const sortOption = ref<SortAction>(grid.location === "allCategories" ? "azAscendingCategory" : (grid.location !== 'credential' ? "azAscendingService" : ""));
const withinCategorySortOption = ref(grid.location === "allCategories" ? "azAscendingService" : "");

// For measuring the length of the pagination
const allPaginatedAndSortedRows = computed(() => {
  if (grid.location === 'credential') return null;

  if (grid.location === 'searchup') {
    let searchSortedResults = sortDecider(sortOption.value, null, grid.rows, grid.credentialMatch);
    return paginateData(searchSortedResults)
  }
  else if (grid.location === 'allCategories') {
    let categoryResults = sortDecider(sortOption.value, null, grid.rows, "");
    return paginateData(categoryResults);
  }
  else {
    let credentialResults = sortDecider(sortOption.value, null, grid.rows, "");
    return paginateData(credentialResults);
  }
});

// Sort and slice per branch: only inside the narrowing is grid.rows one concrete type, so the location and the rows provably belong to the same GridData member.
const modifiedGrid = computed(() => {
  if (grid.location === 'credential') return grid;
  else if (grid.location === 'allCredentials'){
    return {
      location: grid.location,
      rows: paginateData(sortDecider(sortOption.value, null, grid.rows, ""))[pageNumber.value - 1]
    }
  }
  else if (grid.location === 'allCategories'){
    return {
      location: grid.location,
      rows: paginateData(sortDecider(sortOption.value, null, grid.rows, ""))[pageNumber.value - 1]
    } 
  }
  // searchup
  else {
      return {...grid, rows: paginateData(sortDecider(sortOption.value, null, grid.rows, grid.credentialMatch))[pageNumber.value - 1]}
  }
});

const emit = defineEmits(["close", "rerun"]);


// for importing a csv
function loaderHelper(){
  emit('rerun');
}
</script>
