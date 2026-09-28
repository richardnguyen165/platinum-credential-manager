<template>
    <Header></Header>
    <div v-if="isLoading">
        Loading Credentials...
    </div>
    <div v-else-if="!isLoading && !successfullyLoaded">
        Error: Loading credentials failed!
    </div>
    <div v-else>
        <button>
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
    import Header from './Header.vue';
    import axios from 'axios';
    import { onMounted, ref } from 'vue'
    import { keycloak } from '@/config/keycloak';
    import { useRouter } from 'vue-router'
    import useAuth from '@/composables/useAuth'


    const { authState } = useAuth();
    const BACKEND_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5142';
    const allCredentials = ref(null);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const router = useRouter();

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

    const props = defineProps({
        categoryId: Number
    });

    onMounted(async () => {
        try {
            const { data } = await axios.get(`${BACKEND_URL}/category/${props.categoryId}`, {
            headers: { Authorization: `Bearer ${keycloak.token}` }});

            allCredentials.value = data.credentials;
        } catch {
            console.log('Failed to retrive category');
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    });
</script>