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
        <button v-if="!isMiscallaneous" @click="editCategoryModal">
            Edit Category
        </button>
        <button v-if="!isMiscallaneous" @click="deleteCategoryModal">
            Delete Category
        </button>
        <button @click="createCredentialModal">
            Create Credential
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
        <Form :show="showModal" :categoryId="categoryId" :action="action"  :currentEntries="currentEntries" @close="showModal = false" @rerun = "loaderHelper"/>
    </div>
</template>

<script setup>
    import Header from '../layout/Header.vue';
    import { onMounted, ref, reactive } from 'vue'
    import { useRouter } from 'vue-router'
    import { getCategory } from '@/services/categoryService.js';
    import Form from '../ui/Form.vue';

    const isMiscallaneous = ref(null);
    const allCredentials = ref(null);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const router = useRouter();
    const action = ref('');
    const showModal = ref(false);
    const currentEntries = reactive({})

    function redirectBackToAllCategories(){
        router.push(`/cred-categories/${userId}`);
    }

    function redirectCredential(credentialId){
        router.push(`/cred-categories/${credentialId}/${categoryId}/${userId}`);
    }

    function editCategoryModal(){
        action.value = "edit-category";
        showModal.value = true;
    }

    function deleteCategoryModal(){
        action.value = "delete-category";
        showModal.value = true;
    }

    function createCredentialModal(){
        action.value = "create-credential";
        showModal.value = true;
    }

    const { categoryId, userId } = defineProps({
        categoryId: Number,
        userId: Number
    });

    async function loaderHelper() {
        try {
            const data = await getCategory(categoryId);
            allCredentials.value = data.credentials;
            currentEntries.CategoryName = data.categoryName;
            isMiscallaneous.value = currentEntries.CategoryName === "Miscallaneous";
        } catch {
            console.log('Failed to retrive credentals');
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    };

    onMounted(async () => {
        await loaderHelper();
    });
</script>