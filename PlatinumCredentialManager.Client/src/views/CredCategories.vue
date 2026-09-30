<template>
    <Header></Header>
    <div>
        Credential Categories
    </div>
    <div v-if="isLoading">
        Loading Credential Categories...
    </div>
    <div v-else-if="!isLoading && !successfullyLoaded">
        Error: Loading credential categories failed!
    </div>
    <div v-else>
        <button @click="createCategoryModal">Create Category</button>
        <button>Import Category and Credentials</button>
        <button>Export Category and Credentials</button>
        <!-- Only shows category name -->
        <div v-for="category in allCategories" :key="category.id" @click="redirectCredentialCategory(category.id)">
            {{ category.categoryName }}
        </div>
        <Form :show="showModal" :action="action"  @close="showModal = false" @rerun = "categoryLoaderHelper"/>
    </div>
</template>

<script setup>
    import { onMounted, ref } from 'vue';
    import { useRouter } from 'vue-router';

    import Form from '@/components/ui/Form.vue';
    import Header from '@/components/layout/Header.vue';

    import { getUser } from '@/services/userService';
    import useAuth from '@/composables/useAuth';
    import { categoryPath } from '@/utils/routes';

    const allCategories = ref(null);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const action = ref('');
    const showModal = ref(false);
    const router = useRouter();
    const { authState } = useAuth();

    function createCategoryModal() {
        action.value = "create-category";
        showModal.value = true;
    };

    function redirectCredentialCategory(categoryId) {
        router.push(categoryPath(categoryId, authState.userId));
    };

    async function categoryLoaderHelper() {
        try {
            const user = await getUser();
            allCategories.value = user.categories;
        } catch (error) {
            console.error('Failed to retrive credential categories', error);
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    }

    onMounted(categoryLoaderHelper);

    
</script>