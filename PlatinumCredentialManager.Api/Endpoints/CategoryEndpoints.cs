using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Category;

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

        // GET a specific category (when we click on a specific category, we will all of its details and its aname  -> important for frontend)

        // CREATE/POST a category (POST /category)

        // UPDATE/PUT a Category (PUT /category) 

        // DELETE a category (DELETE /category/:id)
        categoryURLGroup.MapDelete("/", async (int id, CredsStoreContext dbContext) =>
        {
            await dbContext.Categories.Where(category => category.Id == id).ExecuteDeleteAsync();
            
            return Results.NoContent(); 
        });
    }
}