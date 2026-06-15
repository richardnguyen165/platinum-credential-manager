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
        // You must create the credential in a category
        credentialURLGroup.MapPost("/", async (CreateCredentialDto newCredential, CredsStoreContext dbContext) =>
        {
            // ?? is the null-coalescing operator. It returns the left side if it's not null, otherwise returns the right side.
            // You create credentials with a category -> pass the id

            // Contradicts itself -> category id can be passed as null (not pass in anything)
            // var category = await dbContext.Categories.FindAsync(newCredential.CategoryId);

            // if (category is null)
            // {
            //     return Results.NotFound();
            // }

            // Check if the category exists
            var categoryCheck = await dbContext.Categories.FindAsync(newCredential.CategoryId);

            if (categoryCheck is null)
            {
                return Results.NotFound();
            }

            // Check if there exists a credential of the same name in the same category
            bool nameTakenStatus = await dbContext.Credentials
            .AnyAsync(credential => 
                (credential.CategoryId == newCredential.CategoryId) 
                && (credential.ServiceName == newCredential.ServiceName)
            );

            if (nameTakenStatus)
            {
                return Results.Conflict("Service name already taken in selected category");
            }

            Credential credential = new()
            {
                Username = newCredential.Username ?? "",
                ServiceName = newCredential.ServiceName,
                Password = newCredential.Password,
                CategoryId = newCredential.CategoryId
            };

            dbContext.Credentials.Add(credential);

            await dbContext.SaveChangesAsync();

            var category = await dbContext.Categories.FindAsync(credential.CategoryId);

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

        // UPDATE/PUT a credenital (PUT /creds/:id)
        credentialURLGroup.MapPut("/{id}", async (int id, UpdateCredentialDto updatedCredential, CredsStoreContext dbContext) =>
        {
            var findCredential = await dbContext.Credentials.FindAsync(id);

            if (findCredential is null)
            {
                return Results.NotFound();
            }

            bool nameTakenStatus = await dbContext.Credentials
            .AnyAsync(credential =>
                // Check if any other credential (EXCLUDING itself, has the same name)
                credential.Id != id
                && (credential.CategoryId == updatedCredential.CategoryId)
                && (credential.ServiceName == updatedCredential.ServiceName)
            );

            if (nameTakenStatus)
            {
                return Results.Conflict("Service name already taken in selected category");
            }

            if (string.IsNullOrWhiteSpace(updatedCredential.Password))
            {
                return Results.BadRequest("Password cannot be blank!");
            }

            findCredential.CategoryId = updatedCredential.CategoryId;

            findCredential.ServiceName = updatedCredential.ServiceName;

            findCredential.Username = updatedCredential.Username;

            findCredential.Password = updatedCredential.Password;

            findCredential.DateLastUpdated = DateOnly.FromDateTime(DateTime.UtcNow);

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