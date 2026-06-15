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
                    // Miscallaneous must be id 1 (it is the default category for credentials).
                    // The 5 categories after it get ids 2..6 in the order they are added.
                    context.Set<Category>().AddRange(
                        new Category { CategoryName = "Miscallaneous" },       // 1
                        new Category { CategoryName = "Financial Passwords" }, // 2
                        new Category { CategoryName = "Home" },                // 3
                        new Category { CategoryName = "Work" },                // 4
                        new Category { CategoryName = "Social Media" },        // 5
                        new Category { CategoryName = "Shopping" }             // 6
                    );

                    // Service names are unique WITHIN a category but may repeat ACROSS categories.
                    // Shared-across-categories names below: "Google", "PayPal", "Netflix", "GitHub".
                    context.Set<Credential>().AddRange(
                        // Miscallaneous (CategoryId defaults to 1)
                        new Credential { ServiceName = "Biking", Password = "1234" },
                        new Credential { ServiceName = "Google", Username = "personal", Password = "misc-google" },

                        // Financial Passwords (2)
                        new Credential { ServiceName = "TD Bank", Username = "rich", Password = "123456", CategoryId = 2 },
                        new Credential { ServiceName = "PayPal", Username = "rich@pay", Password = "fin-paypal", CategoryId = 2 },
                        new Credential { ServiceName = "Google", Username = "billing", Password = "fin-google", CategoryId = 2 },

                        // Home (3)
                        new Credential { ServiceName = "Home", Username = "admin", Password = "456", CategoryId = 3 },
                        new Credential { ServiceName = "Security Camera", Username = "", Password = "home-cam", CategoryId = 3 },
                        new Credential { ServiceName = "Netflix", Username = "family", Password = "home-netflix", CategoryId = 3 },

                        // Work (4)
                        new Credential { ServiceName = "Google", Username = "work@corp", Password = "work-google", CategoryId = 4 },
                        new Credential { ServiceName = "Slack", Username = "rich", Password = "work-slack", CategoryId = 4 },
                        new Credential { ServiceName = "GitHub", Username = "rich-work", Password = "work-github", CategoryId = 4 },

                        // Social Media (5)
                        new Credential { ServiceName = "Google", Username = "rich.social", Password = "social-google", CategoryId = 5 },
                        new Credential { ServiceName = "Instagram", Username = "rich.ig", Password = "social-ig", CategoryId = 5 },
                        new Credential { ServiceName = "GitHub", Username = "rich-oss", Password = "social-github", CategoryId = 5 },

                        // Shopping (6)
                        new Credential { ServiceName = "Amazon", Username = "rich", Password = "shop-amazon", CategoryId = 6 },
                        new Credential { ServiceName = "PayPal", Username = "rich@pay", Password = "shop-paypal", CategoryId = 6 },
                        new Credential { ServiceName = "Netflix", Username = "rich", Password = "shop-netflix", CategoryId = 6 }
                    );
                }
                context.SaveChanges();
            })
        );
    }
}