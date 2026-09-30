import { ref } from "vue"

export const useModal = () => {

    const action = ref('');
    const showModal = ref(false);

    function createModal(modalAction){
        action.value = modalAction;
        showModal.value = true;
    }

    function closeModal(){ 
        showModal.value = false 
    };

    return {
        action,
        showModal,
        createModal,
        closeModal
    }
};