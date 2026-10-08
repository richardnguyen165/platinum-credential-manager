<template>
  <Teleport to="body">
    <!-- emitHelper should not have brackets so that it does not call any code -->
    <Modal
      :show="show"
      :inputs="allInputs"
      :categoryId="categoryId"
      :credentialId="credentialId"
      @close="emit('close')"
      @ok="emitHelper"
    >
      <template #header>
        <h3>{{ title }}</h3>
      </template>

      <template #description>
        <p>{{ description }}</p>
      </template>
    </Modal>
  </Teleport>
</template>

<script setup>
import { ref, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import Modal from "./Modal.vue";

import {
  deleteCategory,
  postCategory,
  putCategory,
} from "@/services/categoryService.js";
import {
  deleteCredential,
  postCredential,
  putCredential,
} from "@/services/credentialService.js";
import { categoriesPath, categoryPath } from "@/utils/routes.js";

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

    case "create-credential":
    case "edit-credential":
      return [
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
        { id: 4, text: "Categories that are left blanked out will be placed in the 'Miscallaneous' category."},
        { id: 5, text: "Categories sharing the same name as another category means that credential will be added into that category."},
        { id: 6, text: "Passwords that are left empty means that credential/row will be rejected."},
        { id: 7, text: "Duplicate credentials will be ignored."},
        { id: 8, text: "Any rejection means the CSV file in full will be rejected, and a corresponding error list will be returned to the user highlighting errors."}
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

// TODO: display error message
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
      break;
    case "delete-credential":
      await deleteCredential(id);
      router.push(categoryPath(categoryId, route.params.user_id));
      return;
    case "allCategories":
    case "allCredentials":
      return;
    default:
      console.error(`Unknown form action ${action}`);
      return;
  }

  // All the other cases do not return, so they run emit regardless
  // Cannot put rerun in Category.vue because it would not reload the page before closing the modal
  emit("rerun");
}
</script>
