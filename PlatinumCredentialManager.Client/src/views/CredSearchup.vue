<template>
    <Header></Header>
    <form @submit.prevent="searchForCredentials">
        <p>Search Area</p>
        <div>
            Category Name: <input type = "text" :id="CategoryName" />
            Service Name: <input type = "text" :id="ServiceName" />

            Create/Update: 
            <select name="createUpdate" :id="CreateUpdate" v-model="createUpdateChoice">
                <option value="create">Create</option>
                <option value="update">Update</option>
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
                Starting Date: <input type="date" :id="startDate" name="start-date" min="2000-01-01" :max="todaysDate()" />
                Ending Date: <input type="date" :id="endDate" name="end-date" min="2000-01-01" :max="todaysDate()" />
            </div>
        </div>
        <button type="submit">Search for Credentials</button>
    </form>
</template>

<script setup>
    import Header from '@/components/layout/Header.vue';
    
    import { onMounted, ref } from 'vue';

    const userDateChoice = ref('noBounds');
    const createUpdateChoice = ref('noCreateUpdate');

    // https://stackoverflow.com/questions/1531093/how-do-i-get-the-current-date-in-javascript
    function todaysDate(){
        var today = new Date();

        var day = String(today.getDate()).padStart(2, '0');
        var month = String(today.getMonth() + 1).padStart(2, '0'); //January is 0!
        var year = today.getFullYear();

        return year + "/" + month + "/" + day;
    }

    // TODO: implement searchForCredentials in the backend
    async function searchForCredentials(){
        return;
    }
</script>