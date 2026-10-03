<template>
    <Teleport to="body">
        <!-- emitHelper should not have brackets so that it does not call any code -->
        <Modal :show="show" :inputs="allInputs" :categoryId="categoryId" :credentialId="credentialId" @close="emit('close')" @ok="emitHelper">
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
    import { computed } from 'vue';
    import { useRouter, useRoute } from 'vue-router';
    import Modal from './Modal.vue';

    import useAuth from '@/composables/useAuth.js';
    import { deleteCategory, postCategory, putCategory } from '@/services/categoryService.js';
    import { deleteCredential, postCredential, putCredential } from '@/services/credentialService.js';
    import { categoriesPath, categoryPath } from '@/utils/routes.js';


    const { show, action, categoryId, credentialId, currentEntries } = defineProps({
        show: Boolean,
        action: String,
        categoryId: Number,
        credentialId: Number,
        currentEntries: Object
    });
    const emit = defineEmits(['close', 'rerun']);

    const router = useRouter();
    const route = useRoute();
    const { authState } = useAuth()

    // Since action changes we need computed
    const title = computed(() => titleHelper());
    const allInputs = computed(() => inputHelper());
    const description = computed(() => descriptionHelper());

    function titleHelper(){
        switch(action) {
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
            default:
                return "";
        }
    }

    function descriptionHelper(){
        switch(action) {
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
            default:
                return "";
        }
    }

    function inputHelper(){
        switch (action) {
            case "create-category":
            case "edit-category":
                return [{ key: 'CategoryName', label: 'Category Name', type: 'text', required: true, value: currentEntries?.CategoryName }];
            
            case "create-credential":
            case "edit-credential":
                return [
                { key: 'ServiceName', label: 'Service Name', type: 'text', required: true, value: currentEntries?.ServiceName },
                { key: 'Username', label: 'Username', type: 'text', required: true, value: currentEntries?.Username },
                { key: 'Password', label: 'Password', type: 'password', required: false, value: currentEntries?.Password },
            ];

            default:
                return [];
        }
    }

    async function emitHelper(data = {}, id = null){
        switch(action) {
            case "create-category":
                await postCategory(data);
                break;
            case "edit-category":
                await putCategory(id, data);
                break;
            case "delete-category":
                await deleteCategory(id);
                router.push(categoriesPath(route.params.user_id))
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
            default:
                console.error(`Unknown form action ${action}`)
                return;
        }

        // All the other cases do not return, so they run emit regardless
        // Cannot put rerun in Category.vue because it would not reload the page before closing the modal
        emit('rerun');
    }

</script>