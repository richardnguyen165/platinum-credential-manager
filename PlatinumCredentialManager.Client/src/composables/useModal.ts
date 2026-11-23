import type { FormAction } from "@/types/formType.js";
import { ref } from "vue"

export const useModal = () => {

    const action: FormAction = ref('');
    const showModal = ref(false);

    function createModal(modalAction: FormAction){
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