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
    
    import { homePath, categoriesPath, searchUpPath } from '@/utils/routes';
    import { computed  } from 'vue';
    import { useRoute, useRouter } from 'vue-router';

    const route = useRoute();
    const router = useRouter();

    // Stores current tab
    const currentTab = computed (() => {
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
                newRoute = categoriesPath(authState.userId);
                break;
            case "search":
                newRoute = searchUpPath(authState.userId);
                break;
            default:
                newRoute = homePath();
        }

        if (redirectParam === 'logout') {
            logout();
        }
        else router.push(newRoute);
    };
</script>