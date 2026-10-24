<template>
  <Teleport to="body">
    <!-- emitHelper should not have brackets so that it does not call any code -->
    <BaseModal
      :show="show"
      :inputs="allInputs"
      :rules="rules"
      :canClose = "canClose"
      @close="emit('close')"
      @ok="emitHelper"
    >
      <template #header>
        <h3>{{ title }}</h3>
      </template>

      <template #description>
        <p>{{ description }}</p>
      </template>

      <template #image v-if="action === 'allCategories' || action === 'allCredentials'">
        <img alt="CSV instructions" v-if="action === 'allCategories'" src="/img/importCategory.png" />
        <img alt="CSV instructions" v-if="action === 'allCredentials'" src="/img/importCredentials.png" />
      </template>

      <template #modal-error-message>
        <p>{{ errorMessage }}</p>
      </template>

      <template #modal-csv-error-count v-if="(action === 'allCategories' || action === 'allCredentials') && JSON.stringify(allCSVRuleViolationInfo) !== JSON.stringify({})">
        <p v-if="allCSVRuleViolationInfo['type'] === 'file'">Please fix the file errors before re-submitting!</p>
        <p v-else-if="allCSVRuleViolationInfo['type'] === 'header'">Please correct the header errors (adding, removing, or spelling correctly) before re-submitting!</p>
        <p v-else>Please make sure that each credential is inputted correctly!</p>
        <ul>
          <li v-for="error in allCSVRuleViolationInfo['errors']">
            {{ error }}
          </li>
        </ul>
      </template>
    </BaseModal>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch, inject } from "vue";
import { useRouter, useRoute } from "vue-router";
import BaseModal from "@/components/ui/base-modal/BaseModal.vue";

import {
  getCategories
} from "@/services/categoryService.js";
import { categoriesPath, categoryPath, credentialPath, searchUpPath } from "@/router/routes.js";
import { serviceHandler } from "@/services/serviceHandler.js";
import { parsePayload } from "@/utils/parsePayload.js";
import { useToast } from "vue-toastification";
import { filterImportCSVResponse } from "@/csv/filterImportCSVResponse.js";
import { descriptionHelper, inputHelper, ruleHelper, titleHelper } from "@/form-helper/formHelpers";

const { show, action, currentEntries, query } = defineProps({
  show: Boolean,
  action: String,
  currentEntries: Object,
  query: Object
});
const emit = defineEmits(["close", "rerun"]);

let errorMessage = ref("");
let allCategories = ref([]);
let categoryId = ref(inject("categoryId"));
let canClose = ref(false);
let allCSVRuleViolationInfo = ref({});

const router = useRouter();
const route = useRoute();

// Since action changes we need computed
const title = computed(() => titleHelper(action));
const allInputs = computed(() => inputHelper(action, allCategories.value, currentEntries, categoryId.value));
const description = computed(() => descriptionHelper(action));
const rules = computed(() => ruleHelper(action));

watch(() => show, async () => {
  errorMessage.value = "";
  allCSVRuleViolationInfo.value = {};
  if (action === 'edit-credential'){
    const { data } = await getCategories();
    allCategories.value = data;
  } else {
    allCategories.value = [];
  }
})

async function emitHelper(payload = {}, id = null) {
  canClose.value = false;
  let response, reparsedPayload;

  switch(action){
    case "edit-category":
    case "edit-credential":
    case "allCredentials":
      reparsedPayload = {
        id,
        "data": payload
      }
      break;
    case "allCategories":
      reparsedPayload = payload;
      break;
    case "delete-category":
    case "delete-credential":
      reparsedPayload = {
        id
      }
      break;
    default:
      reparsedPayload = {...payload};
  }

  switch (action) {
    case "create-category":
    case "create-credential":
    case "edit-category":
      response = await serviceHandler(action, reparsedPayload);
      canClose.value = response.valid;
      if (!canClose.value) {
        errorMessage.value=`${response.data.title}: ${response.data.detail}`
        useToast().error(errorMessage.value);
      } else {
        toastSuccessHelper(action);
      }
      break;
    case "allCategories":
    case "allCredentials":
      response = await serviceHandler(action, reparsedPayload);
      canClose.value = response.valid;
      if (!canClose.value) {
        useToast().error("Invalid CSV import!");
        errorMessage.value=`Please read the following details below to determine fixes to your CSV`;
        const allErrorCounts = filterImportCSVResponse(action, response.data.extensions);
        allCSVRuleViolationInfo.value = allErrorCounts;
      } else {
        toastSuccessHelper(action);
      }
      break;
    case "delete-category":
      response = await serviceHandler(action, reparsedPayload);
      if (!response.valid) {
        errorMessage.value=`${response.data.title}: ${response.data.detail}`
        useToast().error(errorMessage.value);
      } else {
        toastSuccessHelper(action);
        router.push(categoriesPath(route.params.user_id));
      }
      return
    case "edit-credential":
      response = await serviceHandler(action, reparsedPayload);
      canClose.value = response.valid;
      if (!canClose.value) {
        errorMessage.value=`${response.data.title}: ${response.data.detail}`
        useToast().error(errorMessage.value);
      }
      if (canClose.value && reparsedPayload.data.categoryId !== categoryId.value){
        toastSuccessHelper(action);
        if (query){
          router.push({
            path: credentialPath(
              id,
              reparsedPayload.data.categoryId,
              route.params.user_id,
            ),
            query: parsePayload(query)
          });
        } else  {
          router.replace(credentialPath(id, reparsedPayload.data.categoryId, route.params.user_id));
        }
      }
      break;
    case "delete-credential":
      response = await serviceHandler(action, reparsedPayload);
      if (!response.valid) {
        errorMessage.value=`${response.data.title}: ${response.data.detail}`
        useToast().error(errorMessage.value);
      } else {
        toastSuccessHelper(action);
        if (JSON.stringify(query) !== JSON.stringify({})) {
          router.push({
            path: searchUpPath(route.params.user_id),
            query: parsePayload(query)
          });
        } else {
          router.push(categoryPath(categoryId.value, route.params.user_id));
        }
      }
      return;
    default:
      console.error(`Unknown form action ${action}`);
      useToast().error("Error in form: Unknown form action");
      return
  }

  // All the other cases do not return, so they run emit regardless
  // Cannot put rerun in Category.vue because it would not reload the page before closing the modal
  if ((action !== "edit-credential" && canClose.value) || (action === "edit-credential" && reparsedPayload.data.categoryId === categoryId.value && canClose.value)) emit("rerun");
}

function toastSuccessHelper(action) {
  const toast = useToast();

  switch(action){
    case "edit-category":
      toast.success("Category edited successfully!");
      break;
    case "edit-credential":
      toast.success("Credential edited successfuly!");
      break;
    case "allCredentials":
    case "allCategories":
      toast.success("Import successful!");
      break;
    case "delete-category":
      toast.success("Category has been deleted.");
      break;
    case "delete-credential":
      toast.success("Credential has been deleted.");
      break;
    case "create-category":
      toast.success("Category has been created!");
      break;
    case "create-credential":
      toast.success("Credential has been created!");
      break;
  }
}
</script>
