<template>
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
    <select
      name="withinCategorySortOption"
      :id="'withinCategorySort'"
      v-model="withinCategorySortOption"
    >
      <option value="azAscendingService">Service Name (Ascending Order)</option>
      <option value="azDescendingService">
        Service Name (Descending Order)
      </option>
      <option value="dateCreatedAscending">
        Date Created (Ascending Order)
      </option>
      <option value="dateCreatedDescending">
        Date Created (Descending Order)
      </option>
      <option value="dateUpdatedAscending">
        Date Updated (Ascending Order)
      </option>
      <option value="dateUpdatedDescending">
        Date Updated (Descending Order)
      </option>
    </select>
  </div>
</template>

<script setup>
const { location } = defineProps({
    location: String
})

const sortOption = defineModel('sortOption');
const withinCategorySortOption = defineModel('withinCategorySortOption');
</script>
