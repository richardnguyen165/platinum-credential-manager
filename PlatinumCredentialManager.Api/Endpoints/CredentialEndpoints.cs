using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CredentialEndpoints
{
    public static void MapCredentialEndpoints(this WebApplication app)
    {
        var credentialURLGroup = app.MapGroup("/creds");

        // READ/GET All users credentials (GET /creds) (For dashboard view)
        credentialURLGroup.MapGet("/", async (CredsStoreContext dbContext) =>
        {
            await dbContext.Credentials
            .Select(credential => new GetAllCredentialsDto(credential.Id, credential.ServiceName, credential.Username, credential.DateCreated, credential.DateLastUpdated))
            .AsNoTracking()
            .ToListAsync();
        });

        // READ/GET a specific user credential (GET /creds/:id) (For detailed view)
        credentialURLGroup.MapGet("/{id}", async (int id, CredsStoreContext dbContext) =>
        {
            // Find credential by id
            var credential = await dbContext.Credentials.FindAsync(id);

            return credential is null ? Results.NotFound() : Results.Ok(

            );
        });

        // CREATE/PUT a credential (PUT /creds)

        // UPDATE/POST a credenital (POST /creds/:id)

        // DELETE a credential (DELETE /creds/:id)
    }
}