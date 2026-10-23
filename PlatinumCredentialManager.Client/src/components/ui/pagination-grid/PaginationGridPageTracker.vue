<template>
  <button :disabled="pageNumber === 1" @click="checkDecrement">«</button>
  <input
    :id="'inputPageNumber'"
    :value="inputPageNumber"
    @input="emit('update:inputPageNumber', $event.target.value)"
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
const { pageNumber, inputPageNumber, currentData, allData } = defineProps({
  pageNumber: Number,
  inputPageNumber: Number,
  currentData: Array,
  allData: Array,
});

const emit = defineEmits(["update:pageNumber", "update:inputPageNumber", "update:currentData"])

function checkIncrement() {
  if (pageNumber !== allData.length) {
    const incrementedPage = pageNumber + 1;
    emit("update:pageNumber", incrementedPage);
    emit("update:inputPageNumber", incrementedPage);
    emit("update:currentData", allData[incrementedPage - 1]);
  }
}

function checkDecrement() {
  if (pageNumber !== 1) {
    const decrementedPage = pageNumber - 1;
    emit("update:pageNumber", decrementedPage);
    emit("update:inputPageNumber", decrementedPage);
    emit("update:currentData", allData[decrementedPage - 1]);
  }
}

function goToPage() {
  let currentInputPageNumber = inputPageNumber;

  if (!Number.isInteger(inputPageNumber)) {
    currentInputPageNumber = Math.round(inputPageNumber);
  }

  if (currentInputPageNumber > allData.length) {
    emit("update:inputPageNumber", allData.length);
    emit("update:pageNumber", allData.length);
    emit("update:currentData", allData[allData.length - 1]);
    return;
  } else if (currentInputPageNumber < 1) {
    emit("update:inputPageNumber", 1);
    emit("update:pageNumber", 1);
    emit("update:currentData", allData[0]);
    return;
  }
  emit("update:currentData", allData[currentInputPageNumber]);
  emit("update:pageNumber", currentInputPageNumber);
}
</script>
