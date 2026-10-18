import { defineStore } from 'pinia'

export const userStore = defineStore('user', {
    state: () => ({
        userId: null,
        categoryId: null,
        credentialId: null
    }),
    actions: {
        setUserId(userId) {
            this.userId = userId;
        },
        setCategoryId(categoryId){
            this.categoryId = categoryId;
        },
        setCredentialId(credentialId) {
            this.credentialId = credentialId;
        }
    }
});