<template>
    <div>
        <button @click="executeRedirect('home')">
            Home
        </button>

        <button @click="executeRedirect('categories')">
            Credential Categories
        </button>

        <button @click="executeRedirect('search')">
            Search
        </button>

        <button @click="executeRedirect('logout')">
            Log-Out
        </button>
    </div>
</template>

<script setup>
    import useAuth from '@/composables/useAuth';
    import { computed } from 'vue';
    import { useRoute, useRouter } from 'vue-router';

    const route = useRoute();
    const router = useRouter();

    const currentTab = computed(() => {
        if (route.path.startsWith('/cred-homepage')) return 'home'
        if (route.path.startsWith('/cred-categories')) return 'categories'
        if (route.path.startsWith('/cred-searchup')) return 'search'
        return ''
    });

    const { logout, authState } = useAuth();

    function executeRedirect(redirectParam){
        let newRoute = null;
        currentTab.value = redirectParam !== 'logout' ? redirectParam : '';

        switch(redirectParam) {
            case "categories":
                newRoute = `/cred-categories/${authState.userId}`;
                break;
            case "search":
                newRoute = `/cred-searchup/${authState.userId}`;
                break;
            default:
                newRoute = "/";
        }

        if (redirectParam === 'logout') {
            logout();
            return;
        }
        else router.push(newRoute);
    };
</script>