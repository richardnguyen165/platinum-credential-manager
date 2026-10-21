// Install: dotnet add PlatinumCredentialManager.Api package DotNetEnv

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer; // For jwt token
using Microsoft.IdentityModel.Tokens;
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
        policy.WithOrigins("https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();            
    });
});

// https://okyrylchuk.dev/blog/minimal-apis-in-dotnet-8/
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name ="PCMAntiForgeryCookie";
    options.FormFieldName = "AntiFogeryFiled";
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.AddCredsStoreDb();  // Register db context 

builder.Services.AddValidation(); // Allows for annotations to work

// For token services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = "https://localhost:8443/realms/platinum";
    options.Audience = "platinum-api";
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience= true,
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    // .NET 10 emits OpenAPI 3.1 by default, but the bundled Swagger UI mishandles it:
    // path params render as "integer | string" and validation wrongly reports
    // "Required field is not provided" even when a value is entered.
    // Pinning the document to 3.0 makes Swagger UI parse it correctly.
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

var app = builder.Build(); // Move this to after the cors and loading dot env

app.MigrateDb();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Swagger UI syntax
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();   // serves /openapi/v1.json
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("./v1/swagger.json", "Platinum Credential Manager"));
}

// CORS policy
app.UseCors(corsPolicy);   // add this before app.MapGamesEndpoints()

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

// // https://www.roundthecode.com/dotnet-tutorials/how-to-upload-files-asp-net-core-web-api
app.MapGet("antiforgery/token", (IAntiforgery forgeryService, HttpContext context) =>
{
    var tokens = forgeryService.GetAndStoreTokens(context);
    context.Response.Cookies.Append("X-CSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions { HttpOnly = false });

    return Results.Ok();
}).RequireAuthorization();

app.MapUserEndpoints();

app.MapCredentialEndpoints();

app.MapCategoryEndpoints();

await app.RunAsync();
