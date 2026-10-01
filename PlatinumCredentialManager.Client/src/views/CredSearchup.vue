<template>
    <Header></Header>
    <form @submit.prevent="searchForCredentials">
        <p>Search Area</p>
        <div>
            Category Name: <input type = "text" v-model.trim="categoryNameChoice" :id="CategoryName" />
            Service Name: <input type = "text" v-model.trim="serviceNameChoice" :id="ServiceName" />

            Create/Update: 
            <select name="createUpdate" :id="CreateUpdate" v-model="createUpdateChoice">
                <option value="Create">Create</option>
                <option value="Update">Update</option>
            </select>

            Date Filter: 
            <select name="dateFilter" :id="dateFilter" v-model="userDateChoice">
                <option value="lastSevenDays">Last 7 Days</option>
                <option value="lastThirtyDays">Last 30 Days</option>
                <option value="lastYear">Last Year</option>
                <option value="noBounds">No Date Bounds</option>
                <option value="boundedByDates">Bounded by Dates</option>
            </select>


            <div v-show="userDateChoice === 'boundedByDates'">
                Date Bound:
               <!-- https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/input/date --> 
                Starting Date: <input type="date" v-model="startDateChoice" :id="startDate" name="start-date" min="2000-01-01" :max="todaysDate()" />
                Ending Date: <input type="date" v-model="endDateChoice" :id="endDate" name="end-date" min="2000-01-01" :max="todaysDate()" />
            </div>
        </div>
        <div>
            {{ formErrorMessage }}
        </div>
        <button type="submit">Search for Credentials</button>
    </form>
    <div v-if="credentials == null">
        Please search for credentials using the inputs above.
    </div>
    <div v-else-if="credentials.length === 0">
        No credentials found with the following inputs above!
    </div>
    <div v-else>
        <!-- TODO: Display credentials -->
    </div>
</template>

<script setup>
    import Header from '@/components/layout/Header.vue';
    import { searchCredential } from '@/services/credentialService';
    
    import { ref } from 'vue';
    import { useRouter, useRoute } from 'vue-router';
    import useAuth from '@/composables/useAuth';

    const router = useRouter();
    const route = useRoute(); // for query parameters

    const formErrorMessage = ref('');
    const categoryNameChoice = ref('');
    const serviceNameChoice = ref('');
    const userDateChoice = ref('lastSevenDays');
    const createUpdateChoice = ref('create');
    const startDateChoice = ref(null);
    const endDateChoice = ref(null);
    const credentials = ref(null);
    const { authState } = useAuth()

    // https://stackoverflow.com/questions/1531093/how-do-i-get-the-current-date-in-javascript
    function todaysDate(){
        let today = new Date();

        let day = String(today.getDate()).padStart(2, '0');
        let month = String(today.getMonth() + 1).padStart(2, '0'); //January is 0!
        let year = today.getFullYear();

        return year + "-" + month + "-" + day;
    }

    function parsePayload(){

        return {
            "CategoryName": categoryNameChoice.value || null,
            "ServiceName": serviceNameChoice.value || null,
            "CreateUpdateChoice": createUpdateChoice.value,
            "UserDateChoice": userDateChoice.value,
            "StartDate": startDateChoice.value || null,
            "EndDate": endDateChoice.value || null
        }
    }

    async function searchForCredentials(){
        formErrorMessage.value = '';

        if (userDateChoice.value === 'boundedByDates')
        {
            if(!startDateChoice.value && !endDateChoice.value)
            {
                formErrorMessage.value = 'Please input at least one date!';
                return;
            }

            if(startDateChoice.value && endDateChoice.value && endDateChoice.value < startDateChoice.value)
            {
                formErrorMessage.value = 'Start date cannot be greater than end date';
                return;
            }
        }

        const payload = parsePayload();

        credentials.value = await searchCredential(payload);

        // https://serversideup.net/blog/url-query-parameters-with-javascript-vue-2-and-vue-3/
        router.push({ query: payload })
    };

    // Nothing reads the query back (suppose you go back to this page, the refs could be empty)
    // route => reading where you are right now (suppose the user bookmarks the page, the refs restart, and since this query and not parameter, the page will be blank despite the query parameters)
    onMounted(async () => {
        if (Object.keys(router.query).length === 0) return;
        categoryNameChoice.value = route.query.CategoryName ?? '';
        serviceNameChoice.value = route.query.ServiceName ?? '';
        createUpdateChoice.value = route.query.CreateUpdateChoice ?? '';
        userDateChoice.value = route.query.UserDateChoice ?? ''
        startDateChoice.value = route.query.StartDate ?? '';
        endDateChoice.value = route.query.EndDate ?? '';

        const payload = parsePayload();

        credentials.value = await searchCredential(payload);
    });
</script>