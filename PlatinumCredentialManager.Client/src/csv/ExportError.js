export class ExportError extends Error {
    constructor(message){
        super(message);
        this.name = "ExportError";
    }
}