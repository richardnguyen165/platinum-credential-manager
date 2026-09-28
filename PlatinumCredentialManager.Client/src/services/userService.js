import http from "./http";

export async function postNewUser(){
    // Logic of no try and catch block, let the calling parent throw it
    const { data } = await http.post(`/user/me`);
    return data;
}

export async function getUser(){
    const { data } = await http.get(`/user/me`);
    return data; 
}