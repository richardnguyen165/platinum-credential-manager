using System.Security.Claims;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Category;
using PlatinumCredentialManager.Api.Dtos.User;
using PlatinumCredentialManager.Api.Models;
using Superpower.Model;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class UserEndpoints
{
    private const string GetCurrentUserRoute = "GetCurrentUser";

    public static void MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/user").RequireAuthorization();

        userGroup.MapGet("/me", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = KeycloakId(principal);

            if (keycloakId is null) return Results.Unauthorized();

            // Finds user based on keycloak id
            var dto = await dbContext.Users
                .Where(u => u.KeycloakId == keycloakId)
                .Select(u => new GetUserDto( 
                    u.Id,
                    u.KeycloakId,
                    u.Categories.Select(
                        category => 
                        // nested dto
                        new CategorySummaryDto(
                            category.Id, // needed for v-for in frontend for v-key
                            category.CategoryName
                        )
                    ).ToList()
                ))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).WithName(GetCurrentUserRoute);

        // 201 first time, 200 second time
        userGroup.MapPost("/me", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = KeycloakId(principal);

            if (keycloakId is null) return Results.Unauthorized();

            // find user

            var user = await dbContext.Users
                .Include(u => u.Categories)
                .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            var created = user is null;

            if (created)
            {
                user = new User { KeycloakId = keycloakId };
                user.Categories.Add(new Category { CategoryName = "Miscallaneous", User = user, UserId = user.Id });
                dbContext.Users.Add(user);

                // Purpose of code, prevent race conditions due to multiple tab reloading or network issues, which can send the same request, resulting in duplicate creation of users
                try {
                    await dbContext.SaveChangesAsync();
                } catch (DbUpdateException) {
                    dbContext.ChangeTracker.Clear();

                    user = await dbContext.Users
                    .Include(u => u.Categories)
                    .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

                    created = false;
                }
            }

            var dto = new GetUserDto(
                user.Id,
                user.KeycloakId,
                user.Categories.Select(c =>
                // nested dto
                new CategorySummaryDto(
                    c.Id, // needed for v-for in frontend for v-key
                    c.CategoryName
                )).ToList());

            return created
                ? Results.CreatedAtRoute(GetCurrentUserRoute, null, dto)
                : Results.Ok(dto);
        });
    }

    private static string? KeycloakId(ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value
        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}