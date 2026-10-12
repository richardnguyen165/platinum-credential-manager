import http from "./http";
import sendingForm from "./sendingForm";

export async function getAllCredentials() {
    try{
        const response = await http.get(`/creds`);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function getCredential(credId) {
    try{
        const response = await http.get(`/creds/${credId}`);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function postCredential(newData) {
    try{
        const response = await http.post(`/creds`, newData);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function putCredential(credId, changedData){
    try{
        const response = await http.put(`/creds/${credId}`, changedData);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

export async function deleteCredential(credId) {
    try {
        const response = await http.delete(`/creds/${credId}`);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

// https://axios.rest/pages/advanced/request-config => for query parameters
export async function searchCredential(queryParams){
    try {
        const response = await http.get(`/creds/search`, { params: queryParams });
        const { data } = response; 
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}

// https://stackoverflow.com/questions/47630163/axios-post-request-to-send-form-data
export async function importCredentials(file){
    try {
        let bodyFormData = new FormData();
        bodyFormData.append('file', file);

        const response = await sendingForm.post("/creds/import", bodyFormData);
        const { data } = response;  
        return {
            data,
            valid: true,
        };
    } catch (err) {
        return {
            data: err.response?.data,
            valid: false
        }
    }
}