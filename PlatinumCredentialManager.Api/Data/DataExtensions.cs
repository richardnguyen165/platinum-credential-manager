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
                    // Every Category is owned by a User, so seed a placeholder user first and
                    // hang all the demo data off it. KeycloakId stands in for the JWT "sub"
                    // claim - a real one is a GUID issued by Keycloak.
                    var seedUser = new User { KeycloakId = "00000000-0000-0000-0000-000000000000" };
                    context.Set<User>().Add(seedUser);

                    // Miscallaneous must be id 1 (it is the default category for credentials).
                    // The 5 categories after it get ids 2..6 in the order they are added.
                    // Setting the User navigation lets EF fill in UserId on SaveChanges.
                    context.Set<Category>().AddRange(
                        new Category { CategoryName = "Miscallaneous",       User = seedUser }, // 1
                        new Category { CategoryName = "Financial Passwords", User = seedUser }, // 2
                        new Category { CategoryName = "Home",                User = seedUser }, // 3
                        new Category { CategoryName = "Work",                User = seedUser }, // 4
                        new Category { CategoryName = "Social Media",        User = seedUser }, // 5
                        new Category { CategoryName = "Shopping",            User = seedUser }  // 6
                    );

                    // Persist the user + categories now so the categories get their real ids
                    // (1..6, in add order). The credentials below reference categories by the
                    // literal CategoryId, so those rows must exist before the credential inserts.
                    context.SaveChanges();

                    // Service names are unique WITHIN a category but may repeat ACROSS categories.
                    // Shared-across-categories names below: "Google", "PayPal", "Netflix", "GitHub".
                    var seededCredentials = new[]
                    {
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
                    };

                    // Credential.UserId is a required FK; every seeded credential is owned by the
                    // same seed user (mirrors its Category's owner).
                    // foreach (var credential in seededCredentials)
                    // {
                    //     credential.User = seedUser;
                    // }

                    context.Set<Credential>().AddRange(seededCredentials);
                }
                context.SaveChanges();
            })
        );
    }
}