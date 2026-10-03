<template>

    <!-- Button to determine what we need to sort by (for categories and credentials, A-Z, Z-A, (Service Name for credential, Category Name for category) Date Created Ascending, Date Created Descending, Date Updated Ascending, Date Updated Descending) -->
    <!-- For the search, we need to include ALL the options above plus search priority (0 = exact match, 1 = starts with, 2 = contains the text, 3 = no match ). It needs to show when the search is returned  (TODO) -->

    <!-- Things needed, the grid, (determine if we need to show all the categories, all the credentials in a catgory, or the searched category) (DONE) -->

    <!-- For pagination, we need, a back arrow, a page number, a front arrow. We need to store the page to grey out the arrow when it is at a bound. Additionally page input is accepted. -->
     <div v-if="action === 'allCategories'">
        <div v-for="category in allData" :key="category.id" @click="redirectToCredentialsOfCategory(category.id)">
            {{ category.categoryName }}
        </div>
     </div>

     <div v-else-if="action === 'searchup'">
        <div @click="redirectCredentialQuery(credential)" v-for="credential in allData" :key="credential.id">
            <div>
                Service Name: {{ credential.serviceName }}
            </div>
            <div>
                Category Name: {{ credential.categoryName }}
            </div>
            <div>
                Date Created: {{ credential.dateCreated }}
            </div>
            <div>
                Date Last Updated: {{ credential.dateLastUpdated }}
            </div>
        </div>
     </div>

     <div v-else>
        <div @click="redirectCredential(credential.id)" v-for="credential in allData" :key="credential.id">
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
    import { ref } from 'vue';
    import { useRouter, useRoute } from 'next/router.js';

    import { credentialPath } from '@/utils/routes.js';
    import { parsePayload } from '@/utils/parsePayload.js';

    const { categoryId, data, action  } = defineProps({
        categoryId: Number,
        data: Array,
        action: String
    });

    const allData = ref(null);
    const pageNumber = ref(0);

    const router = useRouter();
    const route = useRoute();

    // When in category
    function redirectCredential(credentialId){
        router.push(credentialPath(credentialId, categoryId, route.params.user_id));
    }

    // When in searchup of categories
    function redirectCredentialQuery(credential){
        router.push({ path: credentialPath(credential.credentialId, credential.categoryId, route.params.user_id), query: parsePayload(data) });
    }

    // When in credentials of a category
    function redirectToCredentialsOfCategory(categoryId) {
        router.push(categoryPath(categoryId, route.params.user_id));
    };

    // First, when we reach onMounted => we have the data => we need to first paginate it
    onMounted(() => {
        // TODO: Paginate data in paginate.js
        
        allData.value = data;
    });
</script>