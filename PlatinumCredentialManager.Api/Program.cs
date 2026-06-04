// Install: dotnet add PlatinumCredentialManager.Api package DotNetEnv

using PlatinumCredentialManager.Api.Endpoints;

DotNetEnv.Env.Load();  // reads .env from the working directory

var builder = WebApplication.CreateBuilder(args);

var corsPolicy = "AllowVue";

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        // null forgiving -> ! ->  just in case
        policy.WithOrigins(builder.Configuration["FRONTEND_URL"]!)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build(); // Move this to after the cors and loading dot env

// CORS policy
app.UseCors(corsPolicy);   // add this before app.MapGamesEndpoints()

app.MapCredentialEndpoints();

app.Run();
