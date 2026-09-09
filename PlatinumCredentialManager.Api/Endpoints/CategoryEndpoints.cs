using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Category;
using PlatinumCredentialManager.Api.Dtos.Credential;
using PlatinumCredentialManager.Api.Models;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CategoryEndpoints
{
    private const string GetCategoryEndpointName = "GetCategory";
    private const int RANDOM_STRING_LENGTH = 6;

    // https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
    private static string randomTagGenerator()
    {
        Random random = new Random();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(
            Enumerable
            .Repeat(chars, RANDOM_STRING_LENGTH)
            .Select(character => character[random.Next(character.Length)])
            .ToArray()
        );
    }

    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var categoryURLGroup = app.MapGroup("/category").RequireAuthorization();

        var MISC_CONSTANT = "Miscallaneous";

        // GET All Categories belonging to user
        categoryURLGroup.MapGet("/", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            return Results.Ok(await dbContext.Categories
            .Where(category => category.UserId == user.Id)
            .Select(category => new GetAllCategoryDto(category.CategoryName))
            .AsNoTracking()
            .ToListAsync());
        });

        // GET a specific category (when we click on a specific category, we will all of its details (credentials) and its name  -> important for frontend)
        categoryURLGroup.MapGet("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            // This doesnot join with the Credentials table, so it returns an empty list

            var category = await dbContext.Categories
            .Where(c => c.Id == id && c.UserId == user.Id)
            .Select(c => new GetDetailedCategoryDto(
                c.Id,
                c.CategoryName,
                // hides password
                c.Credentials.Select(cr => new GetAllCredentialsDto(
                    cr.Id,
                    cr.ServiceName,
                    cr.Username,
                    cr.DateCreated,
                    cr.DateLastUpdated
                )).ToList()))
            .AsNoTracking()
            .FirstOrDefaultAsync();
            // FirstOrDefaultAsync actually joins with the Credential table, and finds the related Credentials

            return category is null ? Results.NotFound() : Results.Ok(category);
        });

        // CREATE/POST a category (POST /category)
        categoryURLGroup.MapPost("/", async (CreateCategoryDto newCategory, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            // Category.UserId is a required FK, so we need the caller's User row before saving
            // The "sub" claim is Keycloak user id

            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            // Look up caller, creating the row on the first find
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            var categorySameName = await dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.CategoryName == newCategory.CategoryName.Trim()
                && category.UserId == user.Id
            );

            if (categorySameName is not null)
            {
                return Results.BadRequest("There exists a category that has the same name!");
            }
            else if (newCategory.CategoryName == MISC_CONSTANT)
            {
                return Results.BadRequest("Category name cannot be named 'Miscallaneous'");
            }

            Category category = new()
            {
                CategoryName = newCategory.CategoryName.Trim(),
                User = user,
                UserId = user.Id
            };

            dbContext.Categories.Add(category);

            await dbContext.SaveChangesAsync();

            GetDetailedCategoryDto newCategoryDetails = new (
                category.Id,
                category.CategoryName,
                category.Credentials.Select(cr => new GetAllCredentialsDto(
                    cr.Id,
                    cr.ServiceName,
                    cr.Username,
                    cr.DateCreated,
                    cr.DateLastUpdated
                )).ToList()
            );

            return Results.CreatedAtRoute(GetCategoryEndpointName, new { id = newCategoryDetails.Id }, newCategoryDetails );
        }).WithName(GetCategoryEndpointName);

        // UPDATE/PUT a Category (PUT /category/:id) 
        categoryURLGroup.MapPut("/{id}", async (int id, UpdateCategoryDto updatedCategory, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            // Finds first matching category
            var findCategory = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
            var name = updatedCategory.CategoryName.Trim();

            if (name.Length == 0)
                return Results.BadRequest("Name cannot be zero legnth!");
            if (findCategory is null)
                return Results.NotFound();
            if (string.IsNullOrWhiteSpace(updatedCategory.CategoryName))
                return Results.BadRequest("Category cannot be blank!");
            if (updatedCategory.CategoryName == MISC_CONSTANT)
                return Results.BadRequest("Category name cannot be named 'Miscallaneous'");
            if (await dbContext.Categories.AnyAsync(c => c.UserId == user.Id && c.CategoryName == name))
                return Results.BadRequest("There exists a category that has the same name!");

            findCategory.CategoryName = name;

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE a category (DELETE /category/:id)
        // When you delete you must the two things:
        // One: Ensure that the category they are deleting is not miscallaneous
        // Two: If we delete a category, we migrate those credentials to miscallaneous
        categoryURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Unauthorized();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Unauthorized();

            var category = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == id && category.UserId == user.Id);

            if (category is null) return Results.NotFound();

            if (category.CategoryName == MISC_CONSTANT) return Results.BadRequest("Cannot delete default category 'Miscallaneous!");

            // 1. Find the miscallenous category id (diff for each user)
            var miscId = await dbContext.Categories
                .Where(c => c.UserId == user.Id && c.CategoryName == MISC_CONSTANT)
                .Select(c => c.Id).FirstAsync();

            // 2. Find all the credential names in the miscalleneous category, store in hashset
            var allMiscNames = await dbContext.Credentials
            .Where(c => c.CategoryId == miscId && c.Category.UserId == user.Id)
            .Select(c => c.ServiceName)
            .ToHashSetAsync();

            // 3. Find all the credentials in deleted category
            var deletedCategoryCredentials = await dbContext.Credentials
            .Where(c => c.CategoryId == id && c.Category.UserId == user.Id)
            .ToListAsync();

            // 4. Change id, and change name if needed
            foreach (Credential credential in deletedCategoryCredentials)
            {
                credential.CategoryId = miscId;
                string name = credential.ServiceName;
                // Check if the misc. category acutally contains the name
                while (allMiscNames.Contains(name))
                {
                    name = $"{credential.ServiceName} - {randomTagGenerator()}";
                }
                credential.ServiceName = name;
            }

            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync();
            return Results.NoContent(); 
        });
    }
}