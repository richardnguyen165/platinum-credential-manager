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

builder.Services.AddValidation(); // Allows for annotations to work

builder.Services.AddOpenApi(options =>
{
    // .NET 10 emits OpenAPI 3.1 by default, but the bundled Swagger UI mishandles it:
    // path params render as "integer | string" and validation wrongly reports
    // "Required field is not provided" even when a value is entered.
    // Pinning the document to 3.0 makes Swagger UI parse it correctly.
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

var app = builder.Build(); // Move this to after the cors and loading dot env

// Swagger UI syntax
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   // serves /openapi/v1.json
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Platinum Credential Manager"));
}

app.MigrateDb();

// CORS policy
app.UseCors(corsPolicy);   // add this before app.MapGamesEndpoints()

app.MapCredentialEndpoints();

app.MapCategoryEndpoints();

app.Run();
