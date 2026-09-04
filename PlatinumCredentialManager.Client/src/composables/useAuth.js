import { reactive } from 'vue';

export default function useAuth(){
    const userState = reactive({ authenticated: false, username: '' });
    const login = () => { return }
};