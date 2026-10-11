using System.Globalization;
using System.Security.Claims;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Category;
using PlatinumCredentialManager.Api.Dtos.Credential;
using PlatinumCredentialManager.Api.Models;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CategoryEndpoints
{
    private const string GetCategoryEndpointName = "GetCategory";
    private const int RANDOM_STRING_LENGTH = 6;

    // https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
    private static string randomTagGenerator()
    {
        Random random = new Random();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(
            Enumerable
            .Repeat(chars, RANDOM_STRING_LENGTH)
            .Select(character => character[random.Next(character.Length)])
            .ToArray()
        );
    }

    private static void headerCheck(string[] header, IDictionary<string, int> errorCounts, ref bool hasErrors)
    {
        // Checking cannot proceed without correct number of entries
        if (header.Length < 4)
        {
            errorCounts["missingColumnError"] = 1;
            hasErrors = true;
            return;
        }

        if (header.Length > 4)
        {
            errorCounts["tooManyColumnError"] = 1;    
            hasErrors = true;
            return;
        }

        string[] CSVHEADERS = { "Category Name", "Service Name", "Username", "Password" };

        for (int i = 0; i < 4; i++)
        {
            // expected csv headers
            string lowerCaseTrimmedCSV = CSVHEADERS[i].Trim().ToLower();
            string underscoreCSV = lowerCaseTrimmedCSV.Replace("_", " ");
            string noSpaceCSV = lowerCaseTrimmedCSV.Replace(" ", "");

            // header from imported csv
            string lowerCaseTrimmedColumn = CSVHEADERS[i].Trim().ToLower();

            bool headerEquivalentResult = (lowerCaseTrimmedColumn == lowerCaseTrimmedCSV) && (lowerCaseTrimmedColumn == underscoreCSV) && (lowerCaseTrimmedColumn == noSpaceCSV);

            if (!headerEquivalentResult)
            {
                errorCounts["wrongColumnName"] += 1;
                hasErrors = true;
            }
        }
    }

    private static bool credentialCheck(string[] credential, IDictionary<string, int> errorCounts, HashSet<(string CategoryName, string ServiceName)> allCredentialNames, ref HashSet<(string CategoryName, string ServiceName)> newCredentialNames, ref bool hasErrors)
    {
        bool rowError = false;

        // Checking cannot proceed without correct number of entries
        if (credential.Length < 4)
        {
            errorCounts["missingRowEntryError"] += 1;   
            hasErrors = true;
            rowError = true;
            return rowError;
        }
        if (credential.Length > 4)
        {
            errorCounts["tooManyRowEntriesError"] += 1;
            hasErrors = true;
            rowError = true;
            return rowError;
        }

        // Category
        if (credential[0].Trim().Length == 0)
        {
            errorCounts["noCategoryName"] += 1;
            hasErrors = true;
            rowError = true;
        }
        if (credential[0].Trim().Length > 100)
        {
            errorCounts["categoryNameTooLong"] += 1;
            hasErrors = true;
            rowError = true;
        }

        // Service Name
        if (allCredentialNames.Contains(ValueTuple.Create(credential[0].Trim(), credential[1].Trim())) || newCredentialNames.Contains(ValueTuple.Create(credential[0].Trim(), credential[1].Trim())))
        {
            errorCounts["duplicateServiceName"] += 1;
            hasErrors = true;
            rowError = true;
        }
        // check for duplicate names within a csv
        else
        {
            newCredentialNames.Add(ValueTuple.Create(credential[0].Trim(), credential[1].Trim()));
        }

        if (credential[1].Trim().Length == 0)
        {
            errorCounts["noServiceName"] += 1;
            hasErrors = true;
            rowError = true;
        }
        if (credential[1].Trim().Length > 100)
        {
            errorCounts["serviceNameTooLong"] += 1;
            hasErrors = true;
            rowError = true;
        }

        // Username
        if (credential[2].Trim().Length > 100)
        {
            errorCounts["usernameTooLong"] += 1;
            hasErrors = true;
            rowError = true;
        }

        // Password
        if (credential[3].Trim().Length == 0)
        {
            errorCounts["noPassword"] += 1;
            hasErrors = true;
            rowError = true;
        }
        if (credential[3].Trim().Length > 100)
        {
            errorCounts["passwordTooLong"] += 1;
            hasErrors = true;
            rowError = true;
        }

        return rowError;
    }

    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var categoryURLGroup = app.MapGroup("/category").RequireAuthorization();

        var MISC_CONSTANT = "Miscallaneous";

        // GET All Categories belonging to user
        categoryURLGroup.MapGet("/", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            return Results.Ok(await dbContext.Categories
            .Where(category => category.UserId == user.Id)
            .Select(category => new GetAllCategoryDto(category.CategoryName))
            .AsNoTracking()
            .ToListAsync());
        });

        // needed for exporting everything as a csv
        categoryURLGroup.MapGet("/export", async (CredsStoreContext dbContext, ClaimsPrincipal principal) => {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var allCategoryInformation = await dbContext.Categories
            .Where(c => c.UserId == user.Id)
            .Select(c => new ExportAllCategoriesDto(
                c.Id,
                c.CategoryName,
                c.Credentials.Select(cr => new ExportAllCredentialsDto(
                    cr.Id,
                    cr.ServiceName,
                    cr.Username,
                    cr.Password,
                    cr.DateCreated,
                    cr.DateLastUpdated
                )).ToList()))
            .AsNoTracking()
            .ToListAsync();

            // Returns all categorys with their credentials
            return Results.Ok(allCategoryInformation);
        });

        // GET a specific category (when we click on a specific category, we will all of its details (credentials) and its name  -> important for frontend)
        categoryURLGroup.MapGet("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            // This doesnot join with the Credentials table, so it returns an empty list

            var category = await dbContext.Categories
            .Where(c => c.Id == id && c.UserId == user.Id)
            .Select(c => new GetDetailedCategoryDto(
                c.Id,
                c.CategoryName,
                // hides password
                c.Credentials.Select(cr => new GetAllCredentialsDto(
                    cr.Id,
                    cr.ServiceName,
                    cr.Username,
                    cr.DateCreated,
                    cr.DateLastUpdated
                )).ToList()))
            .AsNoTracking()
            .FirstOrDefaultAsync();
            // FirstOrDefaultAsync actually joins with the Credential table, and finds the related Credentials

            return category is null ? Results.NotFound() : Results.Ok(category);
        });

        // CREATE/POST a category (POST /category)
        categoryURLGroup.MapPost("/", async (CreateCategoryDto newCategory, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            // Category.UserId is a required FK, so we need the caller's User row before saving
            // The "sub" claim is Keycloak user id

            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            // Look up caller, creating the row on the first find
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var categorySameName = await dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.CategoryName == newCategory.CategoryName
                && category.UserId == user.Id
            );

            if (categorySameName is not null)
            {
                return Results.Problem(
                    detail: "There exists a category that has the same name!",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "400 Bad Request"
                );
            }
            else if (newCategory.CategoryName == MISC_CONSTANT)
            {
                return Results.Problem(
                    detail: "Category name cannot be named 'Miscallaneous'",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "400 Bad Request"
                );
            }

            Category category = new()
            {
                CategoryName = newCategory.CategoryName,
                User = user,
                UserId = user.Id
            };

            dbContext.Categories.Add(category);

            await dbContext.SaveChangesAsync();

            GetDetailedCategoryDto newCategoryDetails = new (
                category.Id,
                category.CategoryName,
                category.Credentials.Select(cr => new GetAllCredentialsDto(
                    cr.Id,
                    cr.ServiceName,
                    cr.Username,
                    cr.DateCreated,
                    cr.DateLastUpdated
                )).ToList()
            );

            return Results.CreatedAtRoute(GetCategoryEndpointName, new { id = newCategoryDetails.Id }, newCategoryDetails );
        }).WithName(GetCategoryEndpointName);

        // UPDATE/PUT a Category (PUT /category/:id) 
        categoryURLGroup.MapPut("/{id}", async (int id, UpdateCategoryDto updatedCategory, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            // Finds first matching category
            var findCategory = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
            var name = updatedCategory.CategoryName;

            if (name.Length == 0)
                return Results.BadRequest("Name cannot be zero legnth!");
            if (findCategory is null)
                return Results.NotFound();
            if (string.IsNullOrWhiteSpace(updatedCategory.CategoryName))
                return Results.BadRequest("Category cannot be blank!");
            if (updatedCategory.CategoryName == MISC_CONSTANT)
                return Results.BadRequest("Category name cannot be named 'Miscallaneous'");
            if (await dbContext.Categories.AnyAsync(c => c.UserId == user.Id && c.CategoryName == name))
                return Results.BadRequest("There exists a category that has the same name!");

            findCategory.CategoryName = name;

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE a category (DELETE /category/:id)
        // When you delete you must the two things:
        // One: Ensure that the category they are deleting is not miscallaneous
        // Two: If we delete a category, we migrate those credentials to miscallaneous
        categoryURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var category = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == id && category.UserId == user.Id);

            if (category is null) return Results.Problem(
                detail: "Category not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "404 Not Found"
            );

            if (category.CategoryName == MISC_CONSTANT) return Results.BadRequest("Cannot delete default category 'Miscallaneous!");

            // 1. Find the miscallenous category id (diff for each user)
            var miscId = await dbContext.Categories
                .Where(c => c.UserId == user.Id && c.CategoryName == MISC_CONSTANT)
                .Select(c => c.Id).FirstAsync();

            // 2. Find all the credential names in the miscalleneous category, store in hashset
            var allMiscNames = await dbContext.Credentials
            .Where(c => c.CategoryId == miscId && c.Category.UserId == user.Id)
            .Select(c => c.ServiceName)
            .ToHashSetAsync();

            // 3. Find all the credentials in deleted category
            var deletedCategoryCredentials = await dbContext.Credentials
            .Where(c => c.CategoryId == id && c.Category.UserId == user.Id)
            .ToListAsync();

            // 4. Change id, and change name if needed
            foreach (Credential credential in deletedCategoryCredentials)
            {
                credential.CategoryId = miscId;
                string name = credential.ServiceName;
                // Check if the misc. category acutally contains the name
                while (allMiscNames.Contains(name))
                {
                    name = $"{credential.ServiceName} - {randomTagGenerator()}";
                }
                credential.ServiceName = name;
            }

            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync();
            return Results.NoContent(); 
        });

        // Mass uploading categories and credentials
        categoryURLGroup.MapPost("/import", async ([FromForm] ImportCategoriesDto importedCredentials, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null) return Results.Problem(
                detail: "User authentication expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "401 Unauthorized"
            );

            // storage of errors
            // https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.idictionary-2?view=net-10.0
            IDictionary<string, int> errorCounts = new Dictionary<string, int>();
            IFormFile file = importedCredentials.File;

            errorCounts.Add("emptyfile", 0);
            errorCounts.Add("noCredenitals", 0);

            // Check the header
            errorCounts.Add("missingColumnError", 0);
            errorCounts.Add("tooManyColumnError", 0);

            errorCounts.Add("wrongColumnName", 0);

            // Check the rows
            errorCounts.Add("missingRowEntryError", 0);
            errorCounts.Add("tooManyRowEntriesError", 0);

            errorCounts.Add("noCategoryName", 0);
            errorCounts.Add("categoryNameTooLong", 0);

            errorCounts.Add("noServiceName", 0);
            errorCounts.Add("duplicateServiceName", 0);
            errorCounts.Add("serviceNameTooLong", 0);

            errorCounts.Add("usernameTooLong", 0);

            errorCounts.Add("noPassword", 0);
            errorCounts.Add("passwordTooLong", 0);

            // https://mojoauth.com/dev-guides/parse-and-generate-csv-with-aspnet-core
            // https://stackoverflow.com/questions/58257222/how-can-i-read-data-as-string-by-csvhelper

            List<string[]> allLines = new List<string[]>();

            // Read all lines first
            using var reader = new StreamReader(file.OpenReadStream());
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                while (csv.Read())
                {
                    allLines.Add(csv.Context.Reader.Parser.Record);
                }
            }

            if (allLines.Count == 0)
            {
                errorCounts["emptyfile"] += 1;
                return Results.Problem(
                    detail: "Empty file. Please fill out the file.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "400 Bad Request",
                    extensions: errorCounts.ToDictionary(kv => kv.Key, kv => (object?)kv.Value)
                );
            }

            // Get all categories

            HashSet<string> allCategoryNames = await dbContext.Categories.Where(cr => cr.UserId == user.Id).Select(cr => cr.CategoryName).ToHashSetAsync();

            // We first need to create a tuple set of all the category names and their credentials
            var rows = await dbContext.Credentials
            .Where(cr => cr.Category.UserId == user.Id)
            .Select(cr => ValueTuple.Create(cr.Category.CategoryName, cr.ServiceName))
            .ToListAsync();

            // Create a hashset of category name service name combinations
            HashSet<(string CategoryName, string ServiceName)> allCredentialNames = new(rows);

            // newCategoryNames and newCredentialNames to keep track of as I process row by row
            HashSet<string> newCategoryNames = new HashSet<string>();
            HashSet<(string CategoryName, string ServiceName)> newCredentialNames = new HashSet<(string CategoryName, string ServiceName)>();

            bool hasErrors = false;

            // Validate each line
            int lineCounter = 0;
            foreach (var line in allLines)
            {
                if (lineCounter == 0)
                {
                    headerCheck(line, errorCounts, ref hasErrors);
                }
                else
                {
                    bool rowResult = credentialCheck(line, errorCounts, allCredentialNames, ref newCredentialNames, ref hasErrors);

                    if (rowResult)
                    {
                        string categoryName = line[0].Trim();
                        // If new category in both in the csv and the existing ones
                        if (!allCategoryNames.Contains(categoryName) && !newCategoryNames.Add(categoryName))
                        {
                            newCategoryNames.Add(categoryName);
                        }
                    }
                }

                lineCounter += 1;
            }

            // Determine whether to add or not
            if (hasErrors)
            {
                return Results.Problem(
                    detail: "Errors found in imported CSV.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "400 Bad Request",
                    extensions: errorCounts.ToDictionary(kv => kv.Key, kv => (object?)kv.Value)
                );
            }
            else
            {
                List<Category> allNewCategories = new List<Category>();

                // One: Create new categories if the newCategoriesName has a length greater than zero
                foreach (var categoryName in newCategoryNames)
                {
                    Category category = new()
                    {
                        CategoryName = categoryName,
                        User = user,
                        UserId = user.Id
                    };

                    allNewCategories.Add(category);

                    dbContext.Categories.Add(category);
                }

                await dbContext.SaveChangesAsync();

                // Two: Create new credentials, row by row

                lineCounter = 0;
                foreach (var line in allLines)
                {
                    if (lineCounter == 0)
                    { 
                        lineCounter += 1;
                        break;   
                    }

                    string categoryName = line[0].Trim();
                    string serviceName = line[1].Trim();
                    string username = line[2].Trim();
                    string password = line[3].Trim();

                    Category correlatedCategory;

                    if (newCategoryNames.Contains(categoryName))
                    {
                        correlatedCategory = allNewCategories.Find(cr => cr.CategoryName == categoryName);
                    }
                    else
                    {
                        correlatedCategory = await dbContext.Categories
                            .FirstOrDefaultAsync(cr => cr.CategoryName == categoryName && cr.UserId == user.Id);
                    }

                    Credential credential;

                    if (username is not null)
                    {
                        credential = new()
                        {
                            ServiceName = serviceName,
                            Username = username,
                            Password = password,
                            CategoryId = correlatedCategory.Id,
                            Category = correlatedCategory
                        };   
                    }
                    else
                    {
                        credential = new()
                        {
                            ServiceName = serviceName,
                            Password = password,
                            CategoryId = correlatedCategory.Id,
                            Category = correlatedCategory
                        };   
                    }

                    dbContext.Credentials.Add(credential);

                    lineCounter += 1;
                }

                await dbContext.SaveChangesAsync();

                return Results.Ok("CSV successfully imported!");
            }
        });

    }
}