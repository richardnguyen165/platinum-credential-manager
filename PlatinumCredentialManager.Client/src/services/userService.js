import http from "../interceptors/http";

export async function postNewUser(){
    // Logic of no try and catch block, let the calling parent throw it
    try{
        const response = await http.post(`/user/me`);
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

export async function getUser(){
    try{
        const response = await http.get(`/user/me`);
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