<template>
    <Header></Header>
    <form @submit.prevent="searchForCredentials">
        <p>Search Area</p>
        <div>
            Category Name: <input type = "text" v-model.trim="data['CategoryName']"/>
            Service Name: <input type = "text" v-model.trim="data['ServiceName']"/>

            Create/Update: 
            <select name="createUpdate" v-model="data['CreateUpdateChoice']">
                <option value="Create">Create</option>
                <option value="Update">Update</option>
            </select>

            Date Filter: 
            <select name="dateFilter" v-model="data['UserDateChoice']">
                <option value="lastSevenDays">Last 7 Days</option>
                <option value="lastThirtyDays">Last 30 Days</option>
                <option value="lastYear">Last Year</option>
                <option value="noBounds">No Date Bounds</option>
                <option value="boundedByDates">Bounded by Dates</option>
            </select>

            <div v-show="data['UserDateChoice'] === 'boundedByDates'">
                Date Bound:
               <!-- https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/input/date --> 
                Starting Date: <input type="date" v-model="data['StartDate']" name="start-date" min="2000-01-01" :max="todaysDate()" />
                Ending Date: <input type="date" v-model="data['EndDate']" name="end-date" min="2000-01-01" :max="todaysDate()" />
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
        <div> Found {{ credentials.length }} result{{ credentials.length !== 1 ? 's' : '' }}!k</div>
        <PaginationGrid :rows="credentials" :params="data" action="searchup" :credential-match="data.ServiceName"/>
    </div>
</template>

<script setup>
    import Header from '@/components/layout/Header.vue';
    import PaginationGrid from '@/components/ui/PaginationGrid.vue';

    import { searchCredential } from '@/services/credentialService';    
    import { ref, reactive, onMounted } from 'vue';
    import { useRouter, useRoute } from 'vue-router';
    import { parsePayload } from '@/utils/parsePayload';

    const router = useRouter();
    const route = useRoute(); // for query parameters
    const data = reactive({
        'CategoryName': '',
        'ServiceName': '',
        'UserDateChoice': 'lastSevenDays',
        'CreateUpdateChoice': 'Create',
        'StartDate': null,
        'EndDate': null
    });
    const formErrorMessage = ref('');
    const credentials = ref([]);
    const submittedServiceName = ref('');

    // https://stackoverflow.com/questions/1531093/how-do-i-get-the-current-date-in-javascript
    function todaysDate(){
        let today = new Date();

        let day = String(today.getDate()).padStart(2, '0');
        let month = String(today.getMonth() + 1).padStart(2, '0'); //January is 0!
        let year = today.getFullYear();

        return year + "-" + month + "-" + day;
    }

    async function searchForCredentials(){

        credentials.value = []; // reset

        formErrorMessage.value = '';

        // Clear any possibity of date being sent if the user switches
        if (data["UserDateChoice"] !== 'boundedByDates') {
            data["StartDate"] = null;
            data["EndDate"] = null;
        }

        else if (data["UserDateChoice"] === 'boundedByDates')
        {
            let startingDate = data["StartDate"];
            let endingDate = data["EndDate"];

            if(!startingDate && !endingDate)
            {
                formErrorMessage.value = 'Please input at least one date!';
                return;
            }

            if(startingDate && endingDate && endingDate < startingDate)
            {
                formErrorMessage.value = 'Start date cannot be greater than end date';
                return;
            }
        }

        const payload = parsePayload(data);

        credentials.value = await searchCredential(payload);

        // Updates the sort, since without it, it would use the other service name value, and it would keep updating as typing (only change it when submitting the form)
        submittedServiceName.value = payload['ServiceName'] ?? '';

        // https://serversideup.net/blog/url-query-parameters-with-javascript-vue-2-and-vue-3/
        router.push({ query: payload })
    };

    // Nothing reads the query back (suppose you go back to this page, the refs could be empty)
    // route => reading where you are right now (suppose the user bookmarks the page, the refs restart, and since this query and not parameter, the page will be blank despite the query parameters)
    onMounted(async () => {
        if (Object.keys(route.query).length === 0) return;
        data["CategoryName"] = route.query.CategoryName ?? '';
        data["ServiceName"] = route.query.ServiceName ?? '';
        data["CreateUpdateChoice"] = route.query.CreateUpdateChoice ?? 'lastSevenDays';
        data["UserDateChoice"] = route.query.UserDateChoice ?? 'Create'
        data["StartDate"] = route.query.StartDate ?? '';
        data["EndDate"] = route.query.EndDate ?? '';

        const payload = parsePayload(data);

        credentials.value = await searchCredential(payload);
    });
</script>