using PlatinumCredentialManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PlatinumCredentialManager.Api.Data;

public static class DataExtensions
{
    public static void MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CredsStoreContext>();
        dbContext.Database.Migrate();
    }

    public static void AddCredsStoreDb(this WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("CredsStore");

        builder.Services.AddSqlite<CredsStoreContext>(connString);
    }
}
