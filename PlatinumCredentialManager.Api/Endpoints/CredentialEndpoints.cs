using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Models;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CredentialEndpoints
{
    private const string GetCredEndpointName = "GetCred";

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
                new GetDetailedCredentialDto(
                    credential.Id,
                    credential.ServiceName,
                    credential.Username,
                    credential.Password,
                    credential.DateCreated,
                    credential.DateLastUpdated
                )
            );
        }).WithName(GetCredEndpointName);

        // CREATE/POST a credential (POST /creds)
        credentialURLGroup.MapPost("/", async (CreateCredentialDto newCredential, CredsStoreContext dbContext) =>
        {
            Credential credential = new()
            {
                ServiceName = newCredential.ServiceName,
                Username = newCredential.Username,
                Password = newCredential.Password
            };

            dbContext.Credentials.Add(credential);

            await dbContext.SaveChangesAsync();

            GetDetailedCredentialDto newCredentialDetails = new(
                credential.Id,
                credential.ServiceName,
                credential.Username,
                credential.Password,
                credential.DateCreated,
                credential.DateLastUpdated
            );

            return Results.CreatedAtRoute(GetCredEndpointName, new { id = newCredentialDetails.Id }, newCredentialDetails);
        });

        // UPDATE/PUT a credenital (PUT /creds/:id)
        credentialURLGroup.MapPut("/{id}", async (int id, UpdateCredentialDto updatedCredential, CredsStoreContext dbContext) =>
        {
            var findCredential = await dbContext.Credentials.FindAsync(id);

            if (findCredential is null)
            {
                return Results.NotFound();
            }

            findCredential.ServiceName = updatedCredential.ServiceName;
            findCredential.Username = updatedCredential.Username;
            findCredential.Password =
            updatedCredential.Password;
            findCredential.DateLastUpdated = DateOnly.FromDateTime(DateTime.UtcNow);

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE a credential (DELETE /creds/:id)
        credentialURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext) =>
        {
           await dbContext.Credentials.Where(credential => credential.Id == id).ExecuteDeleteAsync();

           return Results.NoContent(); 
        });
    }
}