import axios from 'axios';
import { createAxios, createConfig, errorResponse } from './requestResponse';

// Source: https://skycloak.io/blog/keycloak-vue-js-authentication-guide/
const http = axios.create(createAxios('http'));

http.interceptors.request.use(
    async (config) => await createConfig(config),
    // responsible for catching errors
    (error) => Promise.reject(error)
);

http.interceptors.response.use((response) => response, async (error) => await errorResponse(error));

export default http;