// utils/routes.js
export const categoriesPath = (userId) => `/cred-categories/${userId}`;
export const categoryPath = (categoryId, userId) => `/cred-categories/${categoryId}/${userId}`;
export const credentialPath = (credentialId, categoryId, userId) => `/cred-categories/${credentialId}/${categoryId}/${userId}`
export const homePath = () => "/";
export const searchUpPath = (userId) => `/cred-searchup/${userId}`;
export const homepagePath = (userId) => `/cred-homepage/${userId}`;