<template>
    <Header></Header>
    <div v-if="isLoading">
        Loading Credentials...
    </div>
    <div v-else-if="!isLoading && !successfullyLoaded">
        Error: Loading credentials failed!
    </div>
    <div v-else>
        <button @click="redirectBackToAllCategories">
            Go Back to All Categories
        </button>
        <!-- TODO: Edit Category Modal -->
        <button>
            Edit Category
        </button>
        <!-- TODO: Delete Category Modal -->
        <button>
            Delete Category
        </button>
        <!-- TODO: Add Category Modal -->
        <button>
            Add Credential
        </button>
        <button>
            Import Credentials
        </button>
        <button>
            Export As CSV
        </button>
        <div @click="redirectCredential(credential.id)" v-for="credential in allCredentials" :key="credential.id">
            <div>
                Service Name: {{ credential.serviceName }}
            </div>
            <div>
                Date Created: {{ credential.dateCreated }}
            </div>
            <div>
                Date Last Updated: {{ credential.dateLastUpdated }}
            </div>
        </div>
    </div>
</template>

<script setup>
    import Header from '../layout/Header.vue';
    import { onMounted, ref } from 'vue'
    import { useRouter } from 'vue-router'
    import useAuth from '@/composables/useAuth'
    import { getCategory } from '@/services/categoryService.js';

    const { authState } = useAuth();
    const allCredentials = ref(null);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const router = useRouter();

    function redirectBackToAllCategories(){
        router.push(`/cred-categories/${authState.userId}`);
    }

    function redirectCredential(credentialId){
        router.push(`/cred-categories/${credentialId}/${props.categoryId}/${authState.userId}`);
    }

    // TODO: Edit category modal
    function editCategoryModal(){
        return;
    }

    // TODO: Delete category modal
    function deleteCategoryModal(){
        return;
    }

    // TODO: Add credential modal
    function addCredentialModal(){
        return;
    }

    const { categoryId } = defineProps({
        categoryId: Number
    });

    onMounted(async () => {
        try {
            const { data } = await getCategory(categoryId);

            allCredentials.value = data.credentials;
        } catch {
            console.log('Failed to retrive category');
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    });
</script>