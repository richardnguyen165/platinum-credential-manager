<template>
  <!-- helps annimates element appearing and disappearing-->
  <Transition name="modal">
    <div v-if="show" class="modal-background">
      <Form @submit.prevent="okPasser">
        <div class="modal-container">
          <BaseModalReadArea 
            :rules="rules"
          >
            <template #header>
              <slot name="header"></slot>
            </template>
            <template #description>
              <slot name="description"></slot>
            </template>
            <template #image> 
              <slot name="image"></slot>
            </template>
          </BaseModalReadArea>

          <BaseModalActionArea 
            :inputs="inputs" 
            v-model:disableButton="disableButton"
            v-model:data="data"
            v-model:errorMessage="errorMessage"
            v-model:file="file"
            @ok="okPasser"
            @close="emit('close')"
          >
            <template #modal-error-message>
              <slot name="modal-error-message"></slot>
            </template>
            <template #modal-csv-error-count>
              <slot name="modal-csv-error-count"></slot>
            </template>
          </BaseModalActionArea>
        </div>
      </Form>
    </div>
  </Transition>
</template>

<script setup>
import { inject, ref, watch } from "vue";
import BaseModalReadArea from "./BaseModalReadArea.vue";
import BaseModalActionArea from "./BaseModalActionArea.vue";

let categoryId = ref(inject('categoryId'));
let credentialId = ref(inject('credentialId'));
let data = ref({});
let file = ref(null);
let errorMessage = ref("");
let disableButton = ref(true);

const { show, inputs, rules, canClose } = defineProps({
  show: Boolean,
  inputs: Object,
  rules: Object,
  canClose: Boolean
});

// Passes up ok and close event
const emit = defineEmits(["ok", "close"]);

function okPasser(){
  emit('ok', collectData(), chooseId());
}

// when submitting a form
watch(() => canClose, (newCanClose) => {
  if (newCanClose) {
    emit('close');
  }
})

// You need to reset defaults once show appears
watch(() => show, (newShow) => {
  if (newShow){
    data.value = {}
    file.value = null;
    errorMessage .value = "";
    disableButton.value = false;

    if (inputs.length > 0 ) {
      for (const input of inputs){
        data[input.key] = input["value"];
      }
      disableButton.value = inputs[0]['type'] === 'file'
    }
  }
})

function collectData() {
  if (inputs.length > 0 && inputs[0].type === 'select'){
    return data.value;
  } else if (inputs.length > 0 && inputs[0].type !== 'file'){
    // change from !== to != because of categoryId is undefined it returns true => convert to loosy as its les stricter
    if (categoryId.value != null && inputs[0].type !== 'select') data.value["categoryId"] = categoryId.value;
    return data.value;
  } else {
    return file.value;
  }
}

function chooseId() {
  if (inputs.length > 0 && inputs[0].type === 'file') return categoryId.value || null;
  return credentialId.value || categoryId.value;
}


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
