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
        <button @click="deleteCredentialModal">
            Delete Credential
        </button>
        <button @click="editCredentialModal">
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
        <Form :show="showModal" :categoryId="categoryId" :credentialId="credId" :action="action"  :currentEntries="currentEntries" @close="showModal = false" @rerun = "credentialLoaderHelper"/>
    </div>
</template>

<script setup>
    import Form from '../ui/Form.vue';
    import Header from '../layout/Header.vue';
    import { ref, onMounted, reactive } from 'vue'; 
    import { useRouter } from 'vue-router';
    import { getCredential } from '@/services/credentialService.js';

    const router = useRouter();
    const action = ref('');
    const showModal = ref(false);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const showPassword = ref(false);
    const currentEntries = reactive({})

    function goBackToCategory() {
        router.push(`/cred-categories/${categoryId}/${userId}`);
    };

    function editCredentialModal(){
        action.value = "edit-credential"
        showModal.value = true;
    }

    function deleteCredentialModal() {
        action.value = "delete-credential"
        showModal.value = true;
    }

    const { categoryId, credId, userId } = defineProps({
        categoryId: Number,
        credId: Number,
        userId: Number
    });

    async function credentialLoaderHelper() {
        try {
            const data = await getCredential(credId);
            
            currentEntries.ServiceName = data.serviceName;
            currentEntries.Username = data.username;
            currentEntries.Password = data.password;
            currentEntries.DateCreated = data.dateCreated;
            currentEntries.DateLastUpdated = data.dateLastUpdated
        } catch {
            console.log('Failed to retrive credential');
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    }

    onMounted(async () => {
        await credentialLoaderHelper();
    });
</script>