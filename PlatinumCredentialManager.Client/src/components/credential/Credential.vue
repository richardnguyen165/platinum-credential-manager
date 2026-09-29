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
                Service Name: {{ serviceName }}
            </p>
            <p>
                Username: {{ username }}
            </p>
            <p>
                Password:
                <input :type=" showPassword ? 'text' : 'password' " :id="password" v-model="password" readonly />
                <button @click="showPassword = !showPassword"> {{ showPassword ? 'Hide' : 'Show' }} Password </button>
            </p>
            <p>
                Date Created: {{ dateCreated }}
            </p>
            <p>
                Date Last Updated: {{ dateLastUpdated }}
            </p>
        </div>
        <Form :show="showModal" :categoryId="categoryId" :credentialId="credId" :action="action"  @close="showModal = false" @rerun = "loaderHelper"/>
    </div>
</template>

<script setup>
    import Form from '../ui/Form.vue';
    import Header from '../layout/Header.vue';
    import { ref, onMounted } from 'vue'; 
    import { useRouter } from 'vue-router';
    import { getCredential } from '@/services/credentialService.js';

    const serviceName = ref(''), username = ref(''), password = ref(''), dateCreated = ref(''), dateLastUpdated = ref('');
    const router = useRouter();
    const action = ref('');
    const showModal = ref(false);
    const isLoading = ref(true);
    const successfullyLoaded = ref(true);
    const showPassword = ref(false);

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

    onMounted(async () => {
        try {
            const data = await getCredential(credId);
            
            serviceName.value = data.serviceName;
            username.value = data.username;
            password.value = data.password;
            dateCreated.value = data.dateCreated;
            dateLastUpdated.value = data.dateLastUpdated
        } catch {
            console.log('Failed to retrive credential');
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    });
</script>