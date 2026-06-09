using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Credential;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Models;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CredentialEndpoints
{
    private const string GetCredEndpointName = "GetCred";

    public static void MapCredentialEndpoints(this WebApplication app)
    {
        var credentialURLGroup = app.MapGroup("/creds");

        // READ/GET All users credentials in (GET /creds) (For dashboard view)
        // For a screen that displays all the credentials (not the screen for each category)
        credentialURLGroup.MapGet("/", async (CredsStoreContext dbContext) =>
        {
            return Results.Ok(await dbContext.Credentials
            .Select(credential =>
            new GetAllCredentialsDto(
                    credential.Id,
                    credential.ServiceName,
                    credential.Username,
                    credential.DateCreated,
                    credential.DateLastUpdated
                )
            )
            .AsNoTracking()
            .ToListAsync());
        });

        // READ/GET a specific user credential (GET /creds/:id) (For detailed view)
        credentialURLGroup.MapGet("/{id}", async (int id, CredsStoreContext dbContext) =>
        {
            // Find credential by id
            var credential = await dbContext.Credentials.FindAsync(id);

            if (credential is null)
            {
                return Results.NotFound();
            }

            var category = await dbContext.Categories.FindAsync(credential.CategoryId);

            if (category is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(
                new GetDetailedCredentialDto(
                    credential.Id,
                    credential.ServiceName,
                    credential.Username,
                    credential.Password,
                    credential.DateCreated,
                    credential.DateLastUpdated,
                    category.CategoryName
                )
            );
        }).WithName(GetCredEndpointName);

        // CREATE/POST a credential (POST /creds)
        credentialURLGroup.MapPost("/", async (CreateCredentialDto newCredential, CredsStoreContext dbContext) =>
        {
            // ?? is the null-coalescing operator. It returns the left side if it's not null, otherwise returns the right side.
            // You create credentials with a category -> pass the id

            var category = await dbContext.Categories.FindAsync(newCredential.CategoryId);

            if (category is null)
            {
                return Results.NotFound();
            }

            Credential credential = new()
            {
                Username = newCredential.Username ?? "",
                ServiceName = newCredential.ServiceName,
                Password = newCredential.Password,
                CategoryId = newCredential.CategoryId ?? 1
            };

            dbContext.Credentials.Add(credential);

            await dbContext.SaveChangesAsync();

            GetDetailedCredentialDto newCredentialDetails = new(
                credential.Id,
                credential.ServiceName,
                credential.Username,
                credential.Password,
                credential.DateCreated,
                credential.DateLastUpdated,
                category.CategoryName
            );

            return Results.CreatedAtRoute(GetCredEndpointName, new { id = newCredentialDetails.Id }, newCredentialDetails);
        });

        // UPDATE/PATCH a credenital (PATCH /creds/:id)
        credentialURLGroup.MapPatch("/{id}", async (int id, UpdateCredentialDto updatedCredential, CredsStoreContext dbContext) =>
        {
            var findCredential = await dbContext.Credentials.FindAsync(id);

            if (findCredential is null)
            {
                return Results.NotFound();
            }

            if (updatedCredential.ServiceName is not null)
            {
                findCredential.ServiceName = updatedCredential.ServiceName;
            }
            if (updatedCredential.Username is not null)
            {
                findCredential.Username = updatedCredential.Username;
            }
            if (updatedCredential.CategoryId is not null)
            {
                // value needed since dto category id is nullable
                findCredential.CategoryId = updatedCredential.CategoryId.Value;
            }
            if (updatedCredential.Password is not null)
            {
                if (string.IsNullOrWhiteSpace(updatedCredential.Password))
                {
                    return Results.BadRequest("Password cannot be blank!");
                }
                findCredential.Password = updatedCredential.Password;
            }
            if (updatedCredential.ServiceName is not null || updatedCredential.Username is not null || updatedCredential.Password is not null || updatedCredential.CategoryId is not null)
            {
                findCredential.DateLastUpdated = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE a credential (DELETE /creds/:id)
        credentialURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext) =>
        {
            await dbContext.Credentials
            .Where(credential => credential.Id == id)
            .ExecuteDeleteAsync();

            return Results.NoContent();
        });
    }
}