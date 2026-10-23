<!-- https://mokkapps.de/blog/simpler-two-way-binding-in-vue-with-define-model -->
<template>
  <button :disabled="pageNumber === 1" @click="checkDecrement">«</button>
  <input
    :id="'inputPageNumber'"
    v-model="inputPageNumber"
    type="number"
    min="1"
    :max="allData.length"
    @keyup.enter="goToPage"
  />
  <button
    :disabled="allData.length === 0 || pageNumber === allData.length"
    @click="checkIncrement"
  >
    »
  </button>
</template>

<script setup>
const { allData } = defineProps({
  allData: Array,
});

const pageNumber = defineModel('pageNumber');
const inputPageNumber = defineModel('inputPageNumber');
const currentData = defineModel('currentData');

function checkIncrement() {
  if (pageNumber.value !== allData.length) {
    const incrementedPage = pageNumber.value + 1;
    pageNumber.value = incrementedPage;
    inputPageNumber.value = incrementedPage;
    currentData.value = allData[incrementedPage - 1];
  }
}

function checkDecrement() {
  if (pageNumber.value !== 1) {
    const decrementedPage = pageNumber.value - 1;
    pageNumber.value = decrementedPage;
    inputPageNumber.value = decrementedPage;
    currentData.value = allData[decrementedPage - 1];
  }
}

function goToPage() {
  let currentInputPageNumber = inputPageNumber.value;

  if (!Number.isInteger(inputPageNumber.value)) {
    currentInputPageNumber = Math.round(inputPageNumber.value);
  }

  if (currentInputPageNumber > allData.length) {
    inputPageNumber.value = allData.length;
    pageNumber.value = allData.length;
    currentData.value = allData[allData.length - 1];
    return;
  } else if (currentInputPageNumber < 1) {
    inputPageNumber.value = 1;
    pageNumber.value = 1;
    currentData.value = allData[0];
    return;
  }
  currentData.value = allData[currentInputPageNumber - 1];
  pageNumber.value = currentInputPageNumber;
}
</script>
