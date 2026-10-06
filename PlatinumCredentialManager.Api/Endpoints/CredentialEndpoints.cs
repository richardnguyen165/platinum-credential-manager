using System.Security.Claims;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Credential;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Models;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CredentialEndpoints
{
    private const string GetCredEndpointName = "GetCred";

    private static IQueryable<Credential> credentialDateLINQFinder(CredsStoreContext dbContext, User user, string CreateUpdateChoice, string? UserDateChoice, string? StartDate, string? EndDate){
        
        int decrementDateAmount = 0;
        
        IQueryable<Credential> data = dbContext.Credentials.Where(credential => credential.Category.UserId == user.Id);

        switch(UserDateChoice) 
        {
            case "lastSevenDays":
                decrementDateAmount = -6; // -6 + today = 7 days back
                break;
            case "lastThirtyDays":
                decrementDateAmount = -29;
                break;
            case "lastYear":
                decrementDateAmount = -1; // using -365 doesnt account for leap years
                break;
            case "boundedByDates":
                if (StartDate != null)
                {
                    data = data.Where(credential => DateOnly.Parse(StartDate) <= (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated));
                }

                //EndDate
                if (EndDate != null)
                {
                    data = data.Where(credential => (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated) <= DateOnly.Parse(EndDate));
                }

                return data;

            default:
                return data;
            
        }

        var todaysDate = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly decrementedDate;

        if (UserDateChoice != "lastYear") decrementedDate = todaysDate.AddDays(decrementDateAmount);
        else decrementedDate = todaysDate.AddYears(-1);

        data = data.Where(credential => decrementedDate <= (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated) && (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated) <= todaysDate);

        return data;
    }

    public static void MapCredentialEndpoints(this WebApplication app)
    {
        var credentialURLGroup = app.MapGroup("/creds").RequireAuthorization();

        credentialURLGroup.MapGet("/export/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) => {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            // Export all credentials in a specific categoryid for csv export
            return Results.Ok(await dbContext.Credentials
            .Where(credential => credential.Category.UserId == user.Id)
            .Where(credential => credential.Category.Id == id)
            .Select(credential =>
            new ExportAllCredentialsDto(
                    credential.Id,
                    credential.ServiceName,
                    credential.Username,
                    credential.Password,
                    credential.DateCreated,
                    credential.DateLastUpdated
                )
            )
            .AsNoTracking()
            .ToListAsync());
        });

        // READ/GET All users credentials in (GET /creds)
        // For export csv to find correlating credentials
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
                    credential.Password,
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
                    category.CategoryName,
                    category.Id
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

            var newCredentialServiceName = newCredential.ServiceName;

            // Check if there exists a credential of the same name in the same category
            bool nameTakenStatus = await dbContext.Credentials
            .AnyAsync(credential => 
                (credential.CategoryId == newCredential.CategoryId) 
                && (credential.ServiceName == newCredentialServiceName)
                && credential.Category.UserId == user.Id
            );

            if (nameTakenStatus)
            {
                return Results.Conflict("Service name already taken in selected category");
            }

            Credential credential = new()
            {
                Username = newCredential.Username ?? "",
                ServiceName = newCredentialServiceName,
                Password = newCredential.Password,
                CategoryId = newCredential.CategoryId,
            };

            dbContext.Credentials.Add(credential);

            await dbContext.SaveChangesAsync();

            var category = await dbContext.Categories.FindAsync(credential.CategoryId);

            GetDetailedCredentialDto newCredentialDetails = new(
                credential.Id,
                newCredentialServiceName,
                credential.Username,
                credential.Password,
                credential.DateCreated,
                credential.DateLastUpdated,
                categoryCheck.CategoryName,
                categoryCheck.Id
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

            var updatedCredentialServiceName = updatedCredential.ServiceName;

            bool nameTakenStatus = await dbContext.Credentials
            .AnyAsync(credential =>
                // Check if any other credential (EXCLUDING itself, has the same name)
                credential.Id != id
                && (credential.CategoryId == updatedCredential.CategoryId)
                && (credential.ServiceName == updatedCredentialServiceName)
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

            findCredential.ServiceName = updatedCredentialServiceName;

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

        credentialURLGroup.MapGet("/search", async ([AsParameters] SearchCredentialDto searchCredential, CredsStoreContext dbContext, ClaimsPrincipal principal) => {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();
            
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            IQueryable<Credential> dateLINQ = credentialDateLINQFinder(dbContext, user, searchCredential.CreateUpdateChoice, searchCredential.UserDateChoice, searchCredential.StartDate, searchCredential.EndDate);

            return Results.Ok(await dateLINQ
            .Where(credential => searchCredential.CategoryName == null || credential.Category.CategoryName == searchCredential.CategoryName ||credential.Category.CategoryName.StartsWith(searchCredential.CategoryName) || credential.Category.CategoryName.Contains(searchCredential.CategoryName))
            .Where(credential => searchCredential.ServiceName == null || credential.ServiceName == searchCredential.ServiceName ||credential.ServiceName.StartsWith(searchCredential.ServiceName) ||
            credential.ServiceName.Contains(searchCredential.ServiceName))
            .Select(credential => new SearchCredentialResultsDto(
                credential.Id,
                credential.Category.Id,
                credential.Category.CategoryName,
                credential.ServiceName,
                credential.DateCreated,
                credential.DateLastUpdated
            ))
            .AsNoTracking()
            .ToListAsync());
        });


    }
}