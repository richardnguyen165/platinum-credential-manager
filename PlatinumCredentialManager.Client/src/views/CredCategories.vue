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
        <!-- TODO: Create category modal -->
        <button @click="createCategoryModal">Create Category</button>
        <button>Import Category and Credentials</button>
        <button>Export Category and Credentials</button>
        <!-- Only shows category name -->
        <div v-for="category in allCategories" :key="category.id" @click="redirectCredentialCategory(category.id)" >
            {{ category.categoryName }}
        </div>
    </div>
</template>

<script setup>
    import Header from '@/components/layout/Header.vue';
    import axios from 'axios';
    import { onMounted, ref } from 'vue'
    import { keycloak } from '@/config/keycloak';
    import { useRouter } from 'vue-router'
    import useAuth from '@/composables/useAuth'
    import { getUser } from '@/services/userService';

    const { authState } = useAuth();
    const BACKEND_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5142';
    const allCategories = ref(null);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const router = useRouter();

    function redirectCredentialCategory(categoryId) {
        router.push(`/cred-categories/${categoryId}/${authState.userId}`);
    };

    // TODO: Create category modal
    function createCategoryModal() {
        return;
    };

    onMounted(async () => {
        try {
            const user = await getUser();

            allCategories.value = user.categories;
        } catch {
            console.log('Failed to retrive credential categories');
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    });

    
</script>