<template>
    <!-- https://www.w3tutorials.net/blog/vuejs-pass-slot-to-child-of-child-component/#1-understanding-slots-in-vuejs-a-quick-refresher -->
    <!-- for title-->
    <div class="modal-header">
        <slot name="header"></slot>
    </div>

    <!--description-->
    <div class="modal-description">
        <slot name="description"></slot>
    </div>

    <div class="modal-image">
        <slot name="image"></slot>
    </div>

    <ul class="modal-rules">
        <li v-for="rule in rules" :key="String(rule.id)">{{ rule.text }}</li>
    </ul>
</template>

<script setup lang="ts">
    import type { Rule } from '@/types/formType.js'; 

    const { rules } = defineProps<{ rules: Rule[] }>();
</script>