<template>
    <Header></Header>
    <div v-if="isLoading">
        Loading Credential...
    </div>
    <div v-else-if="!isLoading && !successfullyLoaded">
        Error: Loading credential failed!
    </div>
    <div v-else>
        <button @click="goBackToCategory()">
            Exit Back to Category
        </button>
        <button @click="createModal('delete-credential')">
            Delete Credential
        </button>
        <button @click="createModal('edit-credential')">
            Edit Credential
        </button>
        <button>
            Import Credentials
        </button>
        <button>
            Export Credentials
        </button>
        <div>
            <p>
                Service Name: {{ currentEntries.ServiceName }}
            </p>
            <p>
                Username: {{ currentEntries.Username }}
            </p>
            <p>
                Password:
                <input :type=" showPassword ? 'text' : 'password' " :id="password" v-model="currentEntries.Password" readonly />
                <button @click="showPassword = !showPassword"> {{ showPassword ? 'Hide' : 'Show' }} Password </button>
            </p>
            <p>
                Date Created: {{ currentEntries.DateCreated }}
            </p>
            <p>
                Date Last Updated: {{ currentEntries.DateLastUpdated }}
            </p>
        </div>
        <Form :show="showModal" :categoryId="categoryId" :credentialId="credId" :action="action"  :currentEntries="currentEntries" @close="closeModal" @rerun = "credentialLoaderHelper"/>
    </div>
</template>

<script setup>

    import { ref, onMounted, reactive } from 'vue'; 
    import { useRouter } from 'vue-router';
    
    import Form from '../ui/Form.vue';
    import Header from '../layout/Header.vue';

    import { getCredential } from '@/services/credentialService.js';
    import { categoryPath } from '@/utils/routes.js';
    import { useModal } from '@/composables/useModal.js';

    const { showModal, action, createModal, closeModal } = useModal();

    const { categoryId, credId, userId } = defineProps({
        categoryId: Number,
        credId: Number,
        userId: Number
    });

    const router = useRouter();

    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const showPassword = ref(false);
    const currentEntries = reactive({})

    function goBackToCategory() {
        router.push(categoryPath(categoryId, userId));
    };

    async function credentialLoaderHelper() {
        try {
            const data = await getCredential(credId);
            
            currentEntries.ServiceName = data.serviceName;
            currentEntries.Username = data.username;
            currentEntries.Password = data.password;
            currentEntries.DateCreated = data.dateCreated;
            currentEntries.DateLastUpdated = data.dateLastUpdated
        } catch (error) {
            console.catch('Failed to retrive credential', error);
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    }

    onMounted(credentialLoaderHelper);
</script>