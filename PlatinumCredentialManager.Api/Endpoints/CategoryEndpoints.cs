using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Category;
using PlatinumCredentialManager.Api.Models;
using Superpower.Model;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CategoryEndpoints
{
    private const string GetCategoryEndpointName = "GetCategory";
    private const int MISCALLANEOUS_ID = 1;
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
        var categoryURLGroup = app.MapGroup("/category");

        // GET All Categories
        categoryURLGroup.MapGet("/", async (CredsStoreContext dbContext) =>
        {
            return Results.Ok(await dbContext.Categories
            .Select(category => new GetAllCategoryDto(category.CategoryName))
            .AsNoTracking()
            .ToListAsync());
        });

        // GET a specific category (when we click on a specific category, we will all of its details (credentials) and its name  -> important for frontend)
        categoryURLGroup.MapGet("/{id}", async (int id, CredsStoreContext dbContext) =>
        {
            // This doesnot join with the Credentials table, so it returns an empty list
            // var category = await dbContext.Categories.FindAsync(id);

            var category = await dbContext.Categories
            .Where(c => c.Id == id)
            .Select(c => new GetDetailedCategoryDto(
                c.Id,
                c.CategoryName,
                c.Credentials
            ))
            .AsNoTracking()
            .FirstOrDefaultAsync();
            // FirstOrDefaultAsync actually joins with the Credential table, and finds the related Credentials

            return category is null ? Results.NotFound() : Results.Ok(category);
        });

        // CREATE/POST a category (POST /category)
        categoryURLGroup.MapPost("/", async (CreateCategoryDto newCategory, CredsStoreContext dbContext) =>
        {
            var categorySameName = await dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.CategoryName == newCategory.CategoryName.Trim()
            );

            if (categorySameName is not null)
            {
                return Results.BadRequest("There exists a category that has the same name!");
            }

            Category category = new()
            {
                CategoryName = newCategory.CategoryName
            };

            dbContext.Categories.Add(category);

            await dbContext.SaveChangesAsync();

            GetDetailedCategoryDto newCategoryDetails = new(
                category.Id,
                category.CategoryName,
                category.Credentials
            );

            return Results.CreatedAtRoute(GetCategoryEndpointName, new { id = newCategoryDetails.Id }, newCategoryDetails );
        }).WithName(GetCategoryEndpointName);

        // UPDATE/PUT a Category (PUT /category/:id) 
        categoryURLGroup.MapPut("/{id}", async (int id, UpdateCategoryDto updatedCategory, CredsStoreContext dbContext) =>
        {
            var findCategory = await dbContext.Categories.FindAsync(id);

            if (findCategory is null)
            {
                return Results.NotFound();
            }

            if (string.IsNullOrWhiteSpace(updatedCategory.CategoryName))
            {
                return Results.BadRequest("Category cannot be blank!");
            }
            else if (updatedCategory.CategoryName == "Miscallaneous")
            {
                return Results.BadRequest("Category name cannot be named 'Miscallaneous'");
            }

            findCategory.CategoryName = updatedCategory.CategoryName;

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE a category (DELETE /category/:id)
        // When you delete you must the two things:
        // One: Ensure that the category they are deleting is not miscallaneous
        // Two: If we delete a category, we migrate those credentials to miscallaneous
        categoryURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext) =>
        {
            if (id == MISCALLANEOUS_ID)
            {
                return Results.BadRequest("Cannot delete default category 'Miscallaneous!");
            }

            // SHELVED: DOES NOT CONSIDER THAT MIGRATING CREDENTIALS COULD HAVE SAME NAME AS IN MISC.
            // // 1. Find Category and its credentials
            // // Where is like an if condition for sql
            // var categoryCredentials = await dbContext.Credentials
            // .Where(c => c.CategoryId == id)
            // .ToListAsync();

            // // 2. Iterate through each of its credentials, saving it to category 1
            // foreach (Credential credential in categoryCredentials)
            // {
            //     credential.CategoryId = 1;
            //     // await dbContext.SaveChangesAsync(); Save everything after you are done (1 trip only)
            // }

            // SHELVED: DOES NOT CONSIDER THAT MIGRATING CREDENTIALS COULD HAVE SAME NAME AS IN MISC.
            // This is quicker however
            // https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete
            // Mass update - first we update the credentials, then we delete the category
            // await dbContext.Credentials
            // .Where(credential => credential.CategoryId == id)
            // .ExecuteUpdateAsync(set => set.SetProperty(cred => cred.CategoryId, 1));

            // 1. Get all names in Miscallaneous
            HashSet<String> allMiscNames = await dbContext.Credentials
            .Where(cred => cred.CategoryId == MISCALLANEOUS_ID)
            .Select(c => c.ServiceName)
            .ToHashSetAsync();

            // 2. Find all the credentials in the category
            var categoryCredentials = await dbContext.Credentials
            .Where(c => c.CategoryId == id)
            .ToListAsync();

            // 3. Change id, and change name if needed
            foreach (Credential credential in categoryCredentials)
            {
                credential.CategoryId = MISCALLANEOUS_ID;
                string name = credential.ServiceName;
                // Check if the misc. category acutally contains the name
                while (allMiscNames.Contains(name))
                {
                    name = $"{credential.ServiceName} - {randomTagGenerator()}";
                }
                credential.ServiceName = name;
            }

            await dbContext.SaveChangesAsync();

            // Delete category
            await dbContext.Categories.Where(category => category.Id == id).ExecuteDeleteAsync();
            
            return Results.NoContent(); 
        });
    }
}