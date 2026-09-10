using System.Security.Claims;
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
        var credentialURLGroup = app.MapGroup("/creds").RequireAuthorization();

        // READ/GET All users credentials in (GET /creds) (For dashboard view)
        // For a screen that displays all the credentials (not the screen for each category)
        credentialURLGroup.MapGet("/", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            return Results.Ok(await dbContext.Credentials
            .Where(credential => credential.Category.UserId == user.Id)
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
        credentialURLGroup.MapGet("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            // Find credential by id
            var credential = await dbContext.Credentials.FirstOrDefaultAsync(c => c.Id == id && c.Category.UserId == user.Id);

            if (credential is null)
            {
                return Results.NotFound();
            }

            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == credential.CategoryId && c.UserId == user.Id);

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
        credentialURLGroup.MapPost("/", async (CreateCredentialDto newCredential, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            // Check if the category exists
            var categoryCheck = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == newCredential.CategoryId && category.UserId == user.Id);

            if (categoryCheck is null)
            {
                return Results.NotFound();
            }

            // Check if there exists a credential of the same name in the same category
            bool nameTakenStatus = await dbContext.Credentials
            .AnyAsync(credential => 
                (credential.CategoryId == newCredential.CategoryId) 
                && (credential.ServiceName == newCredential.ServiceName)
                && credential.Category.UserId == user.Id
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
                CategoryId = newCredential.CategoryId,
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
                categoryCheck.CategoryName
            );

            return Results.CreatedAtRoute(GetCredEndpointName, new { id = newCredentialDetails.Id }, newCredentialDetails);
        });

        // UPDATE/PUT a credenital (PUT /creds/:id)
        credentialURLGroup.MapPut("/{id}", async (int id, UpdateCredentialDto updatedCredential, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            var findCredential = await dbContext.Credentials.FirstOrDefaultAsync(credential => credential.Id == id && credential.Category.UserId == user.Id);

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
                && credential.Category.UserId == user.Id
            );
            bool userOwnsCategory = await dbContext.Categories.AnyAsync(c => c.Id == updatedCredential.CategoryId && c.UserId == user.Id);

            if (!userOwnsCategory)
            {
                return Results.BadRequest("User cannot update category they do not own!");
            }

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

            // Username can be blank
            findCredential.Username = updatedCredential.Username ?? "";

            findCredential.Password = updatedCredential.Password;

            findCredential.DateLastUpdated = DateOnly.FromDateTime(DateTime.UtcNow);

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE a credential (DELETE /creds/:id)
        credentialURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            await dbContext.Credentials
            .Where(credential => credential.Id == id && credential.Category.UserId == user.Id)
            .ExecuteDeleteAsync();

            return Results.NoContent();
        });
    }
}