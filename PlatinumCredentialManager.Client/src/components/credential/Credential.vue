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
        <!-- TODO: Delete Credential Modal-->
        <button>
            Delete Credential
        </button>
        <button>
            Import Credentials
        </button>
        <button>
            Export Credentials
        </button>
        <div>
            <input v-model="serviceName">
                Service Name: {{ serviceName }}
            </input>
            <input v-model="username">
                Username: {{ username }}
            </input>
            <input v-model="password">
                Password: {{ password }}
            </input>
            <div>
                Date Created: {{ dateCreated }}
            </div>
            <div>
                Date Last Updated: {{ dateLastUpdated }}
            </div>
        </div>
        <!-- TODO: Edit Credential Saving Action-->
        <button>
            Save Edits
        </button>
    </div>
</template>

<script setup>
    import Header from './layout/Header.vue/index.js';
    import { ref, watch } from 'vue'; 
    import { keycloak } from '@/config/keycloak';
    import { useRouter } from 'vue-router';
    import { getCredential } from '@/services/credentialService.js';

    const credential = ref(null), serviceName = ref(''), username = ref(''), password = ref(''), dateCreated = ref(''), dateLastUpdated = ref(''), categoryId = ref('');
    const router = useRouter();

    function goBackToCategory() {
        router.push(`/cred-categories/${categoryId.value}/${authState.userId}`);
    };

    function editCredentialAction(){
        return;
    }

    function deleteCredentialModal() {
        return;
    }

    const { credId } = defineProps({
        credId: Number
    });

    onMounted(async () => {
        try {
            const { data } = await getCredential(credId);
            
            credential.value = data;
            serviceName.value = data.serviceName;
            username.value = data.username;
            password.value = data.password;
            dateLastUpdated.value = data.dateLastUpdated
        } catch {
            console.log('Failed to retrive credential');
            successfullyLoaded.value = false;
        } finally {
            isLoading.value = false;
        }
    });
</script>