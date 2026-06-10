// Install: dotnet add PlatinumCredentialManager.Api package DotNetEnv

using PlatinumCredentialManager.Api.Data; // For migrate db and add creds store db
using PlatinumCredentialManager.Api.Endpoints;

DotNetEnv.Env.Load();  // reads .env from the working directory

var builder = WebApplication.CreateBuilder(args);

var corsPolicy = "AllowVue";

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        // null forgiving -> ! ->  just in case
        // dotnet run url
        policy.WithOrigins("http://localhost:5142")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.AddCredsStoreDb();  // Register db context 

var app = builder.Build(); // Move this to after the cors and loading dot env

app.MigrateDb();

// CORS policy
app.UseCors(corsPolicy);   // add this before app.MapGamesEndpoints()

app.MapCredentialEndpoints();

app.MapCategoryEndpoints();

app.Run();
