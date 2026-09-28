<template>
    <Teleport to="body">
        <Modal :show="showModal" :inputs="allInputs" @close="showModal = false" @ok="">
            <template #header>
                <h3>{{ title }}</h3>
            </template>
        </Modal>
    </Teleport>
</template>

<script setup>
    import Modal from './Modal.vue';
    import { ref } from 'vue';

    const { action } = defineProps({
        action: String
    })

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
            { key: 'categoryName', label: 'Category Name', type: 'text', required: true }
        ];
        else if (action === "create-credential" || action === "edit-credential") return [
            { key: 'serviceName', label: 'Service Name', type: 'text', required: true },
            { key: 'username', label: 'Username', type: 'text', required: true },
            { key: 'password', label: 'Password', type: 'password', required: false},
        ];
        return [];
    }

    // Since this will only be activate by a click, then we can show the modal once the button is clicked => we can pass a prop
    const showModal = ref(false);
    const title = ref(titleHelper());
    const allInputs = ref(inputHelper());
    const description = ref(descriptionHelper());

</script>