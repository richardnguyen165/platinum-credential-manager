<template>
    <Teleport to="body">
        <!-- emitHelper should not have brackets so that it does not call any code -->
        <Modal :show="show" :inputs="allInputs" :categoryId="categoryId" :credentialId="credentialId" @close="emit('close')" @ok="emitHelper">
            <template #header>
                <h3>{{ title }}</h3>
            </template>

            <template>
                <p>{{ description }}</p>
            </template>
        </Modal>
    </Teleport>
</template>

<script setup>
    import useAuth from '@/composables/useAuth.js';
    import { deleteCategory, postCategory, putCategory } from '@/services/categoryService.js';
    import Modal from './Modal.vue';
    import { computed } from 'vue';
    import { useRouter } from 'vue-router';
    import { deleteCredential, postCredential, putCredential } from '@/services/credentialService.js';

    // Since action changes we need computed
    const title = computed(() => titleHelper());
    const allInputs = computed(() => inputHelper());
    const description = computed(() => descriptionHelper());
    const router = useRouter();
    const { authState } = useAuth()

    const emit = defineEmits(['close', 'rerun']);

    const { show, action, categoryId, credentialId } = defineProps({
        show: Boolean,
        action: String,
        categoryId: Number || null,
        credentialId: Number || null
    });

    function titleHelper(){
        if (action === "create-category") return "Create A Category";
        else if (action === "edit-category") return "Edit A Category";
        else if (action === "delete-category") return "Delete A Category";
        else if (action === "create-credential") return "Create A Credential";
        else if (action === "edit-credential") return "Edit A Credential";
        return "Delete A Credential"
    }

    function descriptionHelper(){
        if (action === "create-category") return "Please create a category by adding a name.";
        else if (action === "edit-category" || action === "edit-credential") return "Please note that all changes to the input fields will be saved when the OK button is pressed.";
        else if (action === "delete-category") return "Are you sure you want to delete this category?";
        else if (action === "create-credential") return "Please create a credential by filling out the input fields below.";
        return "Are you sure you want to delete this credential?"
    }

    function inputHelper(){
        if (action === "create-category" || action === "edit-category") return [
            { key: 'CategoryName', label: 'Category Name', type: 'text', required: true }
        ];
        else if (action === "create-credential" || action === "edit-credential") return [
            { key: 'ServiceName', label: 'Service Name', type: 'text', required: true },
            { key: 'Username', label: 'Username', type: 'text', required: true },
            { key: 'Password', label: 'Password', type: 'password', required: false},
        ];
        return [];
    }

    async function emitHelper(data = {}, id = null){
        if (action === "create-category") await postCategory(data);
        else if (action === "edit-category") await putCategory(id, data);
        else if (action === "delete-category") await deleteCategory(id);
        else if (action === "create-credential") await postCredential(data);
        else if (action === "edit-credential") await putCredential(id, data);
        else await deleteCredential(id);

        if (action == "delete-category") router.push(`/cred-categories/${authState.userId}`);
        else if (action === "delete-credential") router.push(`/cred-categories/${categoryId}/${userId}`);
        // Cannot put rerun in Category.vue because it would not reload the page before closing the modal
        else emit('rerun');
    }

</script>