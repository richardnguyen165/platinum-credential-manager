import type { FormAction } from "@/types/formType.js";
import { ref, type Ref } from "vue"

export const useModal = () => {

    const action: Ref<FormAction, FormAction> = ref('');
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