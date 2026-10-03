<template>
    <!-- helps annimates element appearing and disappearing-->
    <Transition name="modal">
        <div v-if="show" class="modal-background">
            <Form @submit.prevent="collectData">
                <div class="modal-container">
                    <!-- for title-->
                    <div class="modal-header">
                        <slot name="header"></slot>
                    </div>

                    <div class="modal-description">
                        <slot name="description"></slot>
                    </div>

                    <!-- for inputs -->
                    <div class="modal-body" v-for="input in inputs" :key="input.key">
                        {{ input.label }}
                        <input :type="input.type" :id = "input.key" :required="input.required" v-model.trim="data[input.key]"/>
                    </div>

                    <div class="modal-footer">
                        <button type="submit" @click="() => {
                            emit('ok', collectData(), chooseId())
                            emit('close')
                        }">OK</button>
                        <button @click="emit('close')">Cancel</button>
                    </div>
                </div>
            </Form>
        </div>
    </Transition>
</template>

<script setup>
    import { reactive } from 'vue';
    const data = reactive({});
    // Use reactive to store all of the input keys, and their input values

    // Notes for self:
    /* 
    v-model:trim is shorthand for

    <input
        :value="data[input.key]"
        @input-"data[input.key] = $event.target.value.trim()"
    />
    */

    // Sources
    // https://vuejs.org/examples/#modal
    // https://vuejs.org/guide/built-ins/transition.html

    const { show, inputs, categoryId, credentialId } = defineProps({
        show: Boolean,
        inputs: Object,
        categoryId: Number,
        credentialId: Number
    });

    // Passes up ok and close event
    const emit = defineEmits(['ok', 'close']);

    function collectData() {
        // change from !== to != because of categoryId is undefined it returns true => convert to loosy as its les stricter
        if (categoryId != null) data["categoryId"] = categoryId;
        return data;
    }

    function chooseId() {
        return credentialId || categoryId;
    }
</script>

<style>

.modal-background {
    display: flex;
    justify-content: center;
    align-items: center;

    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(0, 0, 0, 0.5);
    backdrop-filter: blur(4px);
    z-index: 10; 
}

.modal-container{
    width: 300px;
    margin: auto;
    padding: 50px 50px;
    background-color: wheat;
    border-radius: 10px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.33);
    transition: all 0.3s ease;
}

.modal-header h3 {
    margin-top: 0;
    color: #42b983;
}

.modal-enter-from, .modal-leave-to {
    opacity: 0;
}

.modal-enter-active, .modal-leave-active {
    transition: opacity 0.5s ease;
}

</style>