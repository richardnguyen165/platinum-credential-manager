<!-- https://mokkapps.de/blog/simpler-two-way-binding-in-vue-with-define-model -->
<template>
  <button :disabled="pageNumber === 1" @click="checkDecrement">«</button>
  <input
    :id="'inputPageNumber'"
    v-model="inputPageNumber"
    type="number"
    min="1"
    :max="pageTotal"
    @keyup.enter="goToPage"
  />
  <button
    :disabled="pageTotal === 0 || pageNumber === pageTotal"
    @click="checkIncrement"
  >
    »
  </button>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

const { pageTotal } = defineProps<{
  pageTotal: number,
}>();

const pageNumber = defineModel<number>('pageNumber', { required: true });
const inputPageNumber = ref(pageNumber.value);

watch(pageNumber, () => {
  inputPageNumber.value = pageNumber.value;
})
  
function checkIncrement() {
  if (pageNumber.value !== pageTotal) {
    pageNumber.value += 1;
  }
}

function checkDecrement() {
  if (pageNumber.value <= 1) {
    pageNumber.value -= 1;
  }
}

function goToPage() {
  let currentInputPageNumber = inputPageNumber.value;

  if (!Number.isInteger(inputPageNumber.value)) {
    currentInputPageNumber = Math.round(inputPageNumber.value);
  }

  if (currentInputPageNumber > pageTotal) {
    pageNumber.value = pageTotal;
  } else if (currentInputPageNumber < 1) {
    pageNumber.value = 1;
  } else {
    pageNumber.value = currentInputPageNumber;
  }

}
</script>
