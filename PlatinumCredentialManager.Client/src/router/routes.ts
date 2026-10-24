// utils/routes.ts
export const categoriesPath = (userId: number | string): string => `/cred-categories/${userId}`;
export const categoryPath = (categoryId: number | string, userId: number | string): string => `/cred-categories/${categoryId}/${userId}`;
export const credentialPath = (credentialId: number | string, categoryId: number | string, userId:number | string): string => `/cred-categories/${credentialId}/${categoryId}/${userId}`
export const homePath = () => "/";
export const searchUpPath = (userId: number | string):string => `/cred-searchup/${userId}`;
export const homepagePath = (userId: number | string):string => `/cred-homepage/${userId}`;