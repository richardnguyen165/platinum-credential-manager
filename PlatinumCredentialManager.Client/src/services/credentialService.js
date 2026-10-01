import http from "./http"

export async function getAllCredentials() {
    const { data } = await http.get(`/creds`);
    return data;
}

export async function getCredential(credId) {
    const { data } = await http.get(`/creds/${credId}`);
    return data;
}

export async function postCredential(newData) {
    const { data } = await http.post(`/creds`, newData);
    return data;
}

export async function putCredential(credId, changedData){
    const { data } = await http.put(`/creds/${credId}`, changedData);
    return data;
}

export async function deleteCredential(credId) {
    const { data } = await http.delete(`/creds/${credId}`);
    return data;
}

// https://axios.rest/pages/advanced/request-config => for query parameters
export async function searchCredential(queryParams){
    const { data } = await http.get(`/creds/search`, { params: queryParams });
    return data;
}