<template>
  <!-- helps annimates element appearing and disappearing-->
  <Transition name="modal">
    <div v-if="show" class="modal-background">
      <Form @submit.prevent="collectData">
        <div class="modal-container">
          <!-- for title-->
          <div class="modal-header">
            <slot name="header"></slot>
          </div>

          <!--description-->
          <div class="modal-description">
            <slot name="description"></slot>
          </div>

          <div class="modal-image">
            <slot name="image"></slot>
          </div>

          <ul class="modal-rules" v-for="rule in rules" :key="rule.key">
            <li>{{ rule.text }}</li>
          </ul>
          
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
            <p v-if="inputs.length > 0 && inputs[0].type === 'file'">{{ fileMessage }}</p>
          </div>

          <div class="modalError">
            <slot name="modalErrorMessage"></slot>
          </div>

          <div class="modal-footer">
            <button
              type="submit"
              @click="
                () => {
                  emit('ok', collectData(), chooseId());
                  emit('close');
                }
              "
              :disabled="disableButton"
            >
              OK
            </button>
            <button @click="emit('close')">Cancel</button>
          </div>
        </div>
      </Form>
    </div>
  </Transition>
</template>

<script setup>
import { reactive, ref, watch } from "vue";

let data = reactive({});
let file = ref(null);
let fileMessage = ref("");
let errorMessage = ref("");
let disableButton = ref(true);

// Use reactive to store all of the input keys, and their input values

// Notes for self:
/* 
    v-model:trim is shorthand for

    <input
        :value="data[input.key]"
        @input-"data[input.key] = $event.target.value.trim()"
    />
    */

// Sources
// https://vuejs.org/examples/#modal
// https://vuejs.org/guide/built-ins/transition.html

const { show, inputs, rules, categoryId, credentialId } = defineProps({
  show: Boolean,
  inputs: Object,
  rules: Object,
  categoryId: Number,
  credentialId: Number,
});

// Passes up ok and close event
const emit = defineEmits(["ok", "close"]);

function selectedCategory(e){
  data["categoryId"] = e.target.value;
}

// https://vueschool.io/articles/vuejs-tutorials/build-a-file-upload-component-in-vue-js-with-the-composition-api/
function handleFileImport(e){
  errorMessage.value = "";
  console.log(e.target.files);

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
  fileMessage.value = `${inputFile.name.length > 10 ? inputFile.name.slice(0, 10) : inputFile.name}`
}

function collectData() {
  if (inputs[0].type === 'select'){
    return data;
  } else if (inputs.length > 0 && inputs[0].type !== 'file'){
    // change from !== to != because of categoryId is undefined it returns true => convert to loosy as its les stricter
    if (categoryId != null && inputs[0].type !== 'select') data["categoryId"] = categoryId;
    return data;
  } else {
    console.log(file.value);
    return file.value;
  }
}

function chooseId() {
  if (inputs.length > 0 && inputs[0].type === 'file') return categoryId || null;
  return credentialId || categoryId;
}


// You nee.d to reset defaults once show appears
watch(() => show, (newShow) => {
  if (newShow){
    data = {}
    file.value = null;
    fileMessage.value = "";
    errorMessage .value = "";
    disableButton.value = false;

    if (inputs.length > 0 ) {
      for (const input of inputs){
        data[input.key] = input["value"];
      }
      console.log(data);
      disableButton.value = inputs[0]['type'] === 'file'
    }
  }
})

</script>

<style>
.modal-background {
  display: flex;
  justify-content: center;
  align-items: center;

  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  z-index: 10;
}

.modal-container {
  width: 700px;
  margin: auto;
  padding: 50px 50px;
  background-color: wheat;
  border-radius: 10px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.33);
  transition: all 0.3s ease;
}

.modal-header h3 {
  margin-top: 0;
  color: #42b983;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.5s ease;
}
</style>
