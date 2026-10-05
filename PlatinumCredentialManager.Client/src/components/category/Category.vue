<template>
    <Header></Header>
    <div v-if="isLoading">
        Loading Credentials...
    </div>
    <div v-else-if="!isLoading && !successfullyLoaded">
        Error: Loading credentials failed!
    </div>
    <div v-else>
        <p>
            Category Name: {{ currentEntries.CategoryName }}
        </p>
        <button @click="redirectBackToAllCategories">
            Go Back to All Categories
        </button>
        <button v-if="!isMiscallaneous" @click="createModal('edit-category')">
            Edit Category
        </button>
        <button v-if="!isMiscallaneous" @click="createModal('delete-category')">
            Delete Category
        </button>
        <button @click="createModal('create-credential')">
            Create Credential
        </button>
        <button>
            Import Credentials As CSV
        </button>
        <button>
            Export As CSV
        </button>
        <PaginationGrid :categoryId="categoryId" :rows="allCredentials" :action="'allCredentials'"/>
        <Form :show="showModal" :categoryId="categoryId" :action="action"  :currentEntries="currentEntries" @close="closeModal" @rerun = "loaderHelper"/>
    </div>
</template>

<script setup>
    import Header from '../layout/Header.vue';
    import Form from '../ui/Form.vue';
    import PaginationGrid from '../ui/PaginationGrid.vue';

    import { onMounted, ref, reactive } from 'vue'
    import { useRouter, useRoute } from 'vue-router'
    import { getCategory } from '@/services/categoryService.js';
    import { categoriesPath } from '@/utils/routes.js';
    import { useModal } from '@/composables/useModal.js';

    const { showModal, action, createModal, closeModal } = useModal();

    const isMiscallaneous = ref(null);
    const allCredentials = ref(null);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const router = useRouter();
    const route = useRoute();
    const currentEntries = reactive({});

    function redirectBackToAllCategories(){
        router.push(categoriesPath(route.params.user_id));
    }

    const { categoryId } = defineProps({
        categoryId: Number
    });

    async function loaderHelper() {
        try {
            const data = await getCategory(categoryId);
            allCredentials.value = data.credentials;
            currentEntries.CategoryName = data.categoryName;
            isMiscallaneous.value = currentEntries.CategoryName === "Miscallaneous";
        } catch (error) {
            console.error('Failed to retrive credentals', error);
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    };

    onMounted(loaderHelper);
</script>