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

        builder.Services.AddSqlite<CredsStoreContext>(
            connString,
            optionsAction: options => options.UseSeeding((context, _) =>
            {
                // Data seeding
                // If credential table is empty
                if (!context.Set<Credential>().Any())
                {
                    // TODO: connect with category
                    context.Set<Credential>().AddRange(
                        new Credential { ServiceName = "TD Bank", Password = "123456" },
                        new Credential { ServiceName = "Home", Username = "admin", Password = "456"}
                    );
                }
                context.SaveChanges();
            })
        );
    }
}