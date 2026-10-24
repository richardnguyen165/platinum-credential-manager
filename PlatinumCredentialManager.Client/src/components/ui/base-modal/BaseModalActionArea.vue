<template>
    <!-- for inputs -->
    <div class="modal-body" v-for="input in inputs" :key="input.key">
    <label :for="input.key">{{ input.label }}</label>
    <input
        v-if="inputs.length > 0 && inputs[0].type !== 'file' && input.type !== 'select'"
        :type="input.type"
        :id="input.key"
        :required="input.required"
        v-model.trim="data[input.key]"
    />
    <select v-else-if="input.type === 'select'" :id="input.key" name="input.key" @change="selectedCategory"> 
        <option :value="option.id" v-for="option in input.options" :key="option.id" :selected="option.selected">
        {{ option.label }}
        </option>
    </select>
    <input
        v-else
        :type="input.type"
        :id="input.key"
        accept=".csv"
        :required="input.required"
        @change="(e) => handleFileImport(e)"            
    />
    </div>

    <div class="modal-error">
        <slot name="modal-error-message"></slot>
    </div>

    <div class="modal-csv-error-display">
        <slot name="modal-csv-error-count"></slot>
    </div>

    <div class="modal-footer">
    <button
        type="submit"
        @click="(e) => {
          e.preventDefault();
          emit('ok')
        }"
        :disabled="disableButton"
    >
        OK
    </button>
    <button @click="(e) => {
        e.preventDefault();
        emit('close')
    }">Cancel</button>
    </div>
</template>

<script setup>
const emit = defineEmits(["ok", "close"]);

const { inputs } = defineProps({
    inputs: Array
});

const errorMessage = defineModel('errorMessage');
const disableButton = defineModel('disableButton');
const file = defineModel('file');
const data = defineModel('data');

function selectedCategory(e){
  data.value["categoryId"] = Number(e.target.value);
}

// https://vueschool.io/articles/vuejs-tutorials/build-a-file-upload-component-in-vue-js-with-the-composition-api/
function handleFileImport(e){
  errorMessage.value = "";

  const inputFile = e.target.files[0];

  if (inputFile == undefined){
    return;
  }

  const sizeInMB = inputFile.size / (1024 * 1024);
  if (sizeInMB > 5){
    errorMessage.value = `Attepmpted file upload: "${inputFile.name}" exceeds the 5MB limit. Please shorten your file.`
    disableButton.value = true;
    file.value = null;
    return;
  }

  disableButton.value = false;
  file.value = inputFile;
}

</script>