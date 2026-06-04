namespace PlatinumCredentialManager.Api.Endpoints;

public static class CredentialEndpoints
{
    public static void MapCredentialEndpoints(this WebApplication app)
    {
        var credentialURLGroup = app.MapGroup("/creds");

        // READ/GET All users credentials (GET /creds)

        // READ/GET a specific user credential (GET /creds/:id)

        // CREATE/PUT a credential (PUT /creds)

        // UPDATE/POST a credenital (POST /creds/:id)

        // DELETE a credential (DELETE /creds/:id)
    }
}