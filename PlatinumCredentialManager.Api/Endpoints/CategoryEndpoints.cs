using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Category;
using PlatinumCredentialManager.Api.Models;
using Superpower.Model;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CategoryEndpoints
{
    private const string GetCategoryEndpointName = "GetCategory";

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
            var category = await dbContext.Categories.FindAsync(id);

            return category is null ? Results.NotFound() : Results.Ok(
                new GetDetailedCategoryDto(
                    category.Id,
                    category.CategoryName,
                    category.Credentials
                )
            );
        });

        // CREATE/POST a category (POST /category)
        categoryURLGroup.MapPost("/", async (CreateCategoryDto newCategory, CredsStoreContext dbContext) =>
        {
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
        });

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
        categoryURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext) =>
        {
            await dbContext.Categories.Where(category => category.Id == id).ExecuteDeleteAsync();
            
            return Results.NoContent(); 
        });
    }
}