<template>
    <Header></Header>
    <div v-if="isLoading">
        Loading Credential...
    </div>
    <div v-else-if="!isLoading && !successfullyLoaded">
        Error: Loading credential failed!
    </div>
    <div v-else>
        <button>
            Exit
        </button>
        <!-- TODO: Delete Credential Modal-->
        <button>
            Delete Credential
        </button>
        <button>
            Export Credential
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
    import Header from './Header.vue';
    import { ref, watch } from 'vue'; 
    import { keycloak } from '@/config/keycloak';

    const credential = ref(null), serviceName = ref(''), username = ref(''), password = ref(''), dateCreated = ref(''), dateLastUpdated = ref('');

    function editCredentialAction(){
        return;
    }

    function deleteCredentialModal() {
        return;
    }

    const props = defineProps({
        userId: Number,
        credId: Number
    });

    onMounted(async () => {
        try {
            const { data } = await axios.get(`${BACKEND_URL}/creds/${props.credId}`, {
            headers: { Authorization: `Bearer ${keycloak.token}` }});

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