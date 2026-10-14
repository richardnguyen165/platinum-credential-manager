<template>
  <Teleport to="body">
    <!-- emitHelper should not have brackets so that it does not call any code -->
    <Modal
      :show="show"
      :inputs="allInputs"
      :categoryId="categoryId"
      :credentialId="credentialId"
      :rules="rules"
      @close="emit('close')"
      @ok="emitHelper"
    >
      <template #header>
        <h3>{{ title }}</h3>
      </template>

      <template #image v-if="action === 'allCategories' || action === 'allCredentials'">
        <img alt="CSV instructions" v-show="action === 'allCategories'" src="/img/importCategory.png" />
        <img alt="CSV instructions" v-show="action === 'allCredentials'" src="/img/importCredentials.png" />
      </template>

      <template #description>
        <p>{{ description }}</p>
      </template>
    </Modal>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch } from "vue";
import { useRouter, useRoute } from "vue-router";
import Modal from "./Modal.vue";

import {
  deleteCategory,
  getCategories,
  importCategories,
  postCategory,
  putCategory,
} from "@/services/categoryService.js";
import {
  deleteCredential,
  importCredentials,
  postCredential,
  putCredential,
} from "@/services/credentialService.js";
import { categoriesPath, categoryPath, credentialPath } from "@/utils/routes.js";

const { show, action, categoryId, credentialId, currentEntries } = defineProps({
  show: Boolean,
  action: String,
  categoryId: Number,
  credentialId: Number,
  currentEntries: Object,
});
const emit = defineEmits(["close", "rerun"]);

const router = useRouter();
const route = useRoute();

// Since action changes we need computed
const title = computed(() => titleHelper());
const allInputs = computed(() => inputHelper());
const description = computed(() => descriptionHelper());
const rules = computed(() => ruleHelper());

const errorMessage = ref("");
let allCategories = ref([]);

watch(() => show, async () => {
  if (action === 'edit-credential'){
    const { data } = await getCategories();
    allCategories.value = data;
  } else {
    allCategories.value = [];
  }
})

function titleHelper() {
  switch (action) {
    case "create-category":
      return "Create A Category";
    case "edit-category":
      return "Edit A Category";
    case "delete-category":
      return "Delete A Category";
    case "create-credential":
      return "Create A Credential";
    case "edit-credential":
      return "Edit A Credential";
    case "delete-credential":
      return "Delete A Credential";
    case "allCategories":
      return "Import Credentials For Any Category"
    case "allCredentials":
      return "Import Credentials Into A Category"
    default:
      return "";
  }
}

function descriptionHelper() {
  switch (action) {
    case "create-category":
      return "Please create a category by adding a name.";
    case "edit-category":
    case "edit-credential":
      return "Please note that all changes to the input fields will be saved when the OK button is pressed.";
    case "delete-category":
      return "Are you sure you want to delete this category?";
    case "create-credential":
      return "Please create a credential by filling out the input fields below.";
    case "delete-credential":
      return "Are you sure you want to delete this credential?";
    case "allCategories":
    case "allCredentials":
      return "Please follow the CSV format provided in the image below."
    default:
      return "";
  }
}

function inputHelper() {
  const credentialArray = [
        {
          key: "ServiceName",
          label: "Service Name",
          type: "text",
          required: true,
          value: currentEntries?.ServiceName,
        },
        {
          key: "Username",
          label: "Username",
          type: "text",
          required: true,
          value: currentEntries?.Username,
        },
        {
          key: "Password",
          label: "Password",
          type: "password",
          required: false,
          value: currentEntries?.Password,
        },
  ];

  let categoryOptions = {
      key: "CategoryId",
      label: "Category",
      type: "select",
      value: categoryId
  };

  if (action === "edit-credential"){
    const allOptions = [];

    for (const categorySet of allCategories.value){
      allOptions.push({
        label: categorySet["categoryName"],
        id: categorySet["id"],
        selected: categoryId === categorySet["id"]
      })
    }

    categoryOptions["options"] = allOptions;
  }
    
  switch (action) {
    case "create-category":
    case "edit-category":
      return [
        {
          key: "CategoryName",
          label: "Category Name",
          type: "text",
          required: true,
          value: currentEntries?.CategoryName,
        },
      ];
    case "edit-credential":
      return [categoryOptions, ...credentialArray];
    case "create-credential":
      return credentialArray;
    case "allCredentials":
    case "allCategories":
      return [
        {
          key: "importCSV",
          label: "Import CSV",
          type: "file",
          required: true,
          value: "",
        }
      ];
    default:
      return [];
  }
}

/*
Types of errors:

- Malformed error
- Service name duplicated
- Service name empty
- Service name too long
- Password empty
- Password too long


- Allow user to choose an simple error list only with counts
*/
function ruleHelper() {
  switch(action) {
    case "allCategories":
      return [
        { id: 0, text: "Please add the following headers to the first row of the CSV: Category Name, Service Name, Username and Password"},
        { id: 1, text: "Usernames can be blanked out."},
        { id: 2, text: "Serivce names that share the same name as with another credential in a category will be rejected."},
        { id: 3, text: "Service names that are left empty means that credential/row will be rejected."},
        { id: 4, text: "Categories that are left blanked out means that credential/row will be rejected."},
        { id: 5, text: "Credential that have a category sharing the same name as another category means that credential will be added into that category."},
        { id: 6, text: "Passwords that are left empty means that credential/row will be rejected."},
        { id: 7, text: "Credentials that share a same service name with another credential will be ignored."},
        { id: 8, text: "Any rejection means the CSV file in full will be rejected, and a corresponding error count will be returned to the user highlighting errors."}
      ]
    case "allCredentials":
      return [
        { id: 0, text: "Please add the following headers to the first row of the CSV: Service Name, Username and Password"},
        { id: 1, text: "Usernames can be blanked out."},
        { id: 2, text: "Serivce names that share the same name as with another credential in a category will be rejected."},
        { id: 3, text: "Service names that are left empty means that credential/row will be rejected."},
        { id: 4, text: "Credentials that share the same name as with another credential will be rejected."},
        { id: 5, text: "Passwords that are left empty means that credential/row will be rejected."},
        { id: 6, text: "Duplicate credentials will be ignored."},
        { id: 7, text: "Any rejection means the CSV file in full will be rejected, and a corresponding error list will be returned to the user highlighting errors."}
      ]
    default:
      return [];
  }
}

// TODO: display error message => these will eventually need to show the data, and check if it can rerun, and pass to the errorMessage at top
async function emitHelper(data = {}, id = null) {
  switch (action) {
    case "create-category":
      await postCategory(data);
      break;
    case "edit-category":
      await putCategory(id, data);
      break;
    case "delete-category":
      await deleteCategory(id);
      router.push(categoriesPath(route.params.user_id));
      return;
    case "create-credential":
      await postCredential(data);
      break;
    case "edit-credential":
      await putCredential(id, data);
      if (data.categoryId !== categoryId) router.replace(credentialPath(id, data.categoryId, route.params.user_id));
      break;
    case "delete-credential":
      await deleteCredential(id);
      router.push(categoryPath(categoryId, route.params.user_id));
      return;
    case "allCategories":
      await importCategories(data);
      break;
    case "allCredentials":
      await importCredentials(data, id);
      break;
    default:
      console.error(`Unknown form action ${action}`);
      return;
  }

  // All the other cases do not return, so they run emit regardless
  // Cannot put rerun in Category.vue because it would not reload the page before closing the modal
  if (action !== "edit-credential" || (action === "edit-credential" && data.categoryId === categoryId)) emit("rerun");
}
</script>
