using System.Security.Claims;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Credential;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Models;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class CredentialEndpoints
{
    private const string GetCredEndpointName = "GetCred";

    private static void headerCheck(string[] header, IDictionary<string, int> errorCounts, ref bool hasErrors)
    {
        // Checking cannot proceed without correct number of entries
        if (header.Length < 3)
        {
            errorCounts["missingColumnError"] = 3 - header.Length;
            hasErrors = true;
            return;
        }

        if (header.Length > 3)
        {
            errorCounts["tooManyColumnError"] = header.Length - 3;
            hasErrors = true;
            return;
        }

        string[] CSVHEADERS = { "Service Name", "Username", "Password" };

        for (int i = 0; i < 3; i++)
        {
            // expected csv headers
            string lowerCaseTrimmedCsv = CSVHEADERS[i].Trim().ToLower();
            string underscoreCSV = lowerCaseTrimmedCsv.Replace("_", " ");
            string noSpaceCSV = lowerCaseTrimmedCsv.Replace(" ", "");

            // header from imported csv
            string lowerCaseTrimmedColumn = header[i].Trim().ToLower();

            bool headerEquivalentResult = (lowerCaseTrimmedColumn == lowerCaseTrimmedCsv) || (lowerCaseTrimmedColumn == underscoreCSV) || (lowerCaseTrimmedColumn == noSpaceCSV);

            if (!headerEquivalentResult)
            {
                errorCounts["wrongColumnName"]++;
                hasErrors = true;
            }
        }
    }

    private static bool credentialCheck(string[] credential, IDictionary<string, int> errorCounts, HashSet<string> allCredentialNames, ref HashSet<string> newCredentialNames, ref bool hasErrors)
        {
            bool rowError = false;

            // 3 cases:
            // 1 entry => reject
            // 2 entry => accept

            /*
            2 entries has 2 more cases

            [SERVICE_NAME],,[PASSWORD] (accept)
            [SERVICE_NAME],[USERNAME], (reject)
            */

            // Checking cannot proceed without correct number of entries
            // Username can be nulled
            if (credential.Length <= 2)
            {
                errorCounts["missingRowEntryError"]++;
                hasErrors = true;
                rowError = true;
                return rowError;
            }
            if (credential.Length > 3)
            {
                errorCounts["tooManyRowEntriesError"]++;
                hasErrors = true;
                rowError = true;
                return rowError;
            }

            // Service Name
            if (allCredentialNames.Contains(credential[0].Trim()) || newCredentialNames.Contains(credential[0].Trim()))
            {
                errorCounts["duplicateServiceName"]++;
                hasErrors = true;
                rowError = true;
            }
            else
            {
                newCredentialNames.Add(credential[0].Trim());
            }

            if (credential[0].Trim().Length == 0)
            {
                errorCounts["noServiceName"]++;
                hasErrors = true;
                rowError = true;
            }
            if (credential[0].Trim().Length > 100)
            {
                errorCounts["serviceNameTooLong"]++;
                hasErrors = true;
                rowError = true;
            }

            // Username
            if (credential[1].Trim().Length > 100)
            {
                errorCounts["usernameTooLong"]++;
                hasErrors = true;
                rowError = true;
            }

            // Password
            if (credential[2].Trim().Length == 0)
            {
                errorCounts["noPassword"]++;
                hasErrors = true;
                rowError = true;
            }
            if (credential[2].Trim().Length > 100)
            {
                errorCounts["passwordTooLong"]++;
                hasErrors = true;
                rowError = true;
            }

            return rowError;
        }
    private static IQueryable<Credential> credentialDateLINQFinder(CredsStoreContext dbContext, User user, string CreateUpdateChoice, string? UserDateChoice, string? StartDate, string? EndDate){
        int decrementDateAmount = 0;

        IQueryable<Credential> data = dbContext.Credentials.Where(credential => credential.Category.UserId == user.Id);

        switch(UserDateChoice)
        {
            case "lastSevenDays":
                decrementDateAmount = -6; // -6 + today = 7 days back
                break;
            case "lastThirtyDays":
                decrementDateAmount = -29;
                break;
            case "lastYear":
                decrementDateAmount = -1; // using -365 doesnt account for leap years
                break;
            case "boundedByDates":
                if (StartDate != null)
                {
                    data = data.Where(credential => DateOnly.Parse(StartDate) <= (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated));
                }

                //EndDate
                if (EndDate != null)
                {
                    data = data.Where(credential => (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated) <= DateOnly.Parse(EndDate));
                }

                return data;

            default:
                return data;
        }

        var todaysDate = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly decrementedDate;

        if (UserDateChoice != "lastYear") decrementedDate = todaysDate.AddDays(decrementDateAmount);
        else decrementedDate = todaysDate.AddYears(-1);

        data = data.Where(credential => decrementedDate <= (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated) && (CreateUpdateChoice == "Create" ? credential.DateCreated : credential.DateLastUpdated) <= todaysDate);

        return data;
    }

    public static void MapCredentialEndpoints(this WebApplication app)
    {
        var credentialURLGroup = app.MapGroup("/creds").RequireAuthorization();

        // READ/GET All users credentials in (GET /creds)
        // For export csv to find correlating credentials
        // TODO: CSV import endpoint needs a server-side file size check (max 1MB)
        // TODO Create csv import endpoint

        credentialURLGroup.MapGet("/", async (CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized"
                );
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized"
                );
            }

            return Results.Ok(await dbContext.Credentials
            .Where(credential => credential.Category.UserId == user.Id)
            .Select(credential =>
            new GetDetailedCredentialDto(
                    credential.Id,
                    credential.ServiceName,
                    credential.Username,
                    credential.Password,
                    credential.DateCreated,
                    credential.DateLastUpdated,
                    credential.Category.CategoryName,
                    credential.Category.Id
                )
            )
            .AsNoTracking()
            .ToListAsync());
        });

        // READ/GET a specific user credential (GET /creds/:id) (For detailed view)
        credentialURLGroup.MapGet("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized"
                );
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized"
                );
            }

            // Find credential by id
            var credential = await dbContext.Credentials.FirstOrDefaultAsync(c => c.Id == id && c.Category.UserId == user.Id);

            if (credential is null)
            {
                return Results.Problem(
                    detail: "Credential not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "404 Not Found"
                );
            }

            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == credential.CategoryId && c.UserId == user.Id);

            if (category is null)
            {
                return Results.Problem(
                    detail: "Category not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "404 Not Found"
                );
            }

            return Results.Ok(
                new GetDetailedCredentialDto(
                    credential.Id,
                    credential.ServiceName,
                    credential.Username,
                    credential.Password,
                    credential.DateCreated,
                    credential.DateLastUpdated,
                    category.CategoryName,
                    category.Id
                )
            );
        }).WithName(GetCredEndpointName);

        // CREATE/POST a credential (POST /creds)
        // You must create the credential in a category
        credentialURLGroup.MapPost("/", async (CreateCredentialDto newCredential, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized");
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized");
            }

            // Check if the category exists
            var categoryCheck = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == newCredential.CategoryId && category.UserId == user.Id);

            if (categoryCheck is null)
            {
                return Results.Problem(
                    detail: "Category does not exist.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "404 Not Found"
                );
            }

            var newCredentialServiceName = newCredential.ServiceName;

            // Check if there exists a credential of the same name in the same category
            bool nameTakenStatus = await dbContext.Credentials
            .AnyAsync(credential => 
                (credential.CategoryId == newCredential.CategoryId) 
                && (credential.ServiceName == newCredentialServiceName)
                && credential.Category.UserId == user.Id
            );

            if (nameTakenStatus)
            {
                return Results.Problem(
                    detail: "Service name already taken in selected category",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "409 Conflict"
                );
            }

            Credential credential = new()
            {
                Username = newCredential.Username ?? "",
                ServiceName = newCredentialServiceName,
                Password = newCredential.Password,
                CategoryId = newCredential.CategoryId,
            };

            dbContext.Credentials.Add(credential);

            await dbContext.SaveChangesAsync();

            var category = await dbContext.Categories.FindAsync(credential.CategoryId);

            GetDetailedCredentialDto newCredentialDetails = new(
                credential.Id,
                newCredentialServiceName,
                credential.Username,
                credential.Password,
                credential.DateCreated,
                credential.DateLastUpdated,
                categoryCheck.CategoryName,
                categoryCheck.Id
            );

            return Results.CreatedAtRoute(GetCredEndpointName, new { id = newCredentialDetails.Id }, newCredentialDetails);
        });

        // UPDATE/PUT a credenital (PUT /creds/:id)
        credentialURLGroup.MapPut("/{id}", async (int id, UpdateCredentialDto updatedCredential, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized");
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized");
            }

            var findCredential = await dbContext.Credentials.FirstOrDefaultAsync(credential => credential.Id == id && credential.Category.UserId == user.Id);

            if (findCredential is null)
            {
                return Results.Problem(
                    detail: "Credential not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "404 Not Found"
                );
            }

            var updatedCredentialServiceName = updatedCredential.ServiceName;

            bool userOwnsCategory = await dbContext.Categories.AnyAsync(c => c.Id == updatedCredential.CategoryId && c.UserId == user.Id);

            if (!userOwnsCategory)
            {
                return Results.Problem(
                    detail:"User cannot update a credential they do not own!",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "400 Bad Request"
                );
            }

            bool nameTakenStatus = await dbContext.Credentials
            .AnyAsync(credential =>
                // Check if any other credential (EXCLUDING itself, has the same name)
                credential.Id != id
                && (credential.CategoryId == updatedCredential.CategoryId)
                && (credential.ServiceName == updatedCredentialServiceName)
                && credential.Category.UserId == user.Id
            );

            if (nameTakenStatus)
            {
                return Results.Problem(
                    detail: "Service name already taken in selected category",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "409 Conflict"
                );
            }

            if (string.IsNullOrWhiteSpace(updatedCredential.Password))
            {
                return Results.Problem(
                    detail: "Password cannot be blank!",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "400 Bad Request"
                );
            }

            findCredential.CategoryId = updatedCredential.CategoryId;

            findCredential.ServiceName = updatedCredentialServiceName;

            // Username can be blank
            findCredential.Username = updatedCredential.Username ?? "";

            findCredential.Password = updatedCredential.Password;

            findCredential.DateLastUpdated = DateOnly.FromDateTime(DateTime.UtcNow);

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE a credential (DELETE /creds/:id)
        credentialURLGroup.MapDelete("/{id}", async (int id, CredsStoreContext dbContext, ClaimsPrincipal principal) =>
        {
            var keycloakId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (keycloakId is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized");
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user is null)
            {
                return Results.Problem(
                    detail: "User authentication expired.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "401 Unauthorized");
            }

            await dbContext.Credentials
            .Where(credential => credential.Id == id && credential.Category.UserId == user.Id)
            .ExecuteDeleteAsync();

            return Results.NoContent();
        });

        credentialURLGroup.MapGet("/search",
            async ([AsParameters] SearchCredentialDto searchCredential, CredsStoreContext dbContext,
                ClaimsPrincipal principal) =>
            {
                var keycloakId = principal.FindFirst("sub")?.Value ??
                                 principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (keycloakId is null)
                {
                    return Results.Problem(
                        detail: "User authentication expired.",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "401 Unauthorized"
                    );
                }

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

                if (user is null)
                {
                    return Results.Problem(
                        detail: "User authentication expired.",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "401 Unauthorized"
                    );
                }

                IQueryable<Credential> dateLINQ = credentialDateLINQFinder(dbContext, user,
                    searchCredential.CreateUpdateChoice, searchCredential.UserDateChoice, searchCredential.StartDate,
                    searchCredential.EndDate);

                return Results.Ok(await dateLINQ
                    .Where(credential => searchCredential.CategoryName == null ||
                                         credential.Category.CategoryName == searchCredential.CategoryName ||
                                         credential.Category.CategoryName.StartsWith(searchCredential.CategoryName) ||
                                         credential.Category.CategoryName.Contains(searchCredential.CategoryName))
                    .Where(credential => searchCredential.ServiceName == null ||
                                         credential.ServiceName == searchCredential.ServiceName ||
                                         credential.ServiceName.StartsWith(searchCredential.ServiceName) ||
                                         credential.ServiceName.Contains(searchCredential.ServiceName))
                    .Select(credential => new SearchCredentialResultsDto(
                        credential.Id,
                        credential.Category.Id,
                        credential.Category.CategoryName,
                        credential.ServiceName,
                        credential.DateCreated,
                        credential.DateLastUpdated
                    ))
                    .AsNoTracking()
                    .ToListAsync());
            });

        // Mass uploading credentials
        // https://www.thatsoftwaredude.com/codebytes/14090/how-to-parse-csv-files-in-c-a-quick-guide
        // https://andrewlock.net/reading-json-and-binary-data-from-multipart-form-data-sections-in-aspnetcore/
        credentialURLGroup.MapPost("/import/{categoryId}",
            async (int categoryId, [FromForm] ImportCredentialsDto importedCredentials, CredsStoreContext dbContext,
                ClaimsPrincipal principal) =>
            {
                var keycloakId = principal.FindFirst("sub")?.Value ??
                                 principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (keycloakId is null)
                {
                    return Results.Problem(
                        detail: "User authentication expired.",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "401 Unauthorized"
                    );
                }

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

                if (user is null)
                {
                    return Results.Problem(
                        detail: "User authentication expired.",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "401 Unauthorized"
                    );
                }

                Dictionary<string, int> errorCounts = new Dictionary<string, int>();
                IFormFile file = importedCredentials.File;

                errorCounts.Add("emptyFile", 0);
                errorCounts.Add("noCredentials", 0);

                // Check the header
                errorCounts.Add("missingColumnError", 0);
                errorCounts.Add("tooManyColumnError", 0);

                errorCounts.Add("wrongColumnName", 0);

                // Check the rows
                errorCounts.Add("missingRowEntryError", 0);
                errorCounts.Add("tooManyRowEntriesError", 0);

                errorCounts.Add("noServiceName", 0);
                errorCounts.Add("duplicateServiceName", 0);
                errorCounts.Add("serviceNameTooLong", 0);

                errorCounts.Add("usernameTooLong", 0);

                errorCounts.Add("noPassword", 0);
                errorCounts.Add("passwordTooLong", 0);

                List<string[]> allLines = new List<string[]>();

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
                    errorCounts["emptyFile"]++;
                    return Results.Problem(
                        detail: "Empty file. Please fill out the file.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "400 Bad Request",
                        extensions: errorCounts.ToDictionary(kv => kv.Key, kv => (object?)kv.Value)
                    );
                }

                HashSet<string> allCredentialNames = await dbContext.Credentials
                    .Where(cr => cr.Category.UserId == user.Id && cr.Category.Id == categoryId)
                    .Select(cr => cr.ServiceName)
                    .ToHashSetAsync();

                HashSet<string> newCredentialNames = new HashSet<string>();

                bool hasErrors = false;
                bool hasCredentials = false;

                // Validate each line
                int lineCounter = 0;
                foreach (var line in allLines)
                {
                    if (lineCounter == 0)
                    {
                        headerCheck(line, errorCounts, ref hasErrors);
                        if (hasErrors) break; // Breaking this so that header errors are corrected first
                    }
                    else
                    {
                        hasCredentials = true;
                        bool rowResult = credentialCheck(line, errorCounts, allCredentialNames, ref newCredentialNames,
                            ref hasErrors);

                        if (!rowResult)
                        {
                            string serviceName = line[0].Trim();
                            // If new category in both in the csv and the existing ones
                            if (!allCredentialNames.Contains(serviceName) && !newCredentialNames.Contains(serviceName))
                            {
                                newCredentialNames.Add(serviceName);
                            }
                        }
                    }

                    lineCounter++;
                }

                if (!hasCredentials)
                {
                    errorCounts["noCredentials"]++;
                    hasErrors = true;
                }

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
                    // One: Create new credentials, row by row

                    lineCounter = 0;
                    foreach (var line in allLines)
                    {
                        if (lineCounter == 0)
                        {
                            lineCounter += 1;
                            continue;
                        }

                        string serviceName = line[0].Trim();
                        string username = line[1].Trim();
                        string password = line[2].Trim();

                        Category correlatedCategory = await dbContext.Categories
                            .FirstOrDefaultAsync(cr => cr.Id == categoryId && cr.UserId == user.Id);

                        Credential credential;

                        if (username != "")
                        {
                            credential = new()
                            {
                                ServiceName = serviceName,
                                Username = username,
                                Password = password,
                                CategoryId = categoryId,
                                Category = correlatedCategory
                            };
                        }
                        else
                        {
                            credential = new()
                            {
                                ServiceName = serviceName,
                                Password = password,
                                CategoryId = categoryId,
                                Category = correlatedCategory
                            };
                        }

                        dbContext.Credentials.Add(credential);
                    }

                    await dbContext.SaveChangesAsync();

                    return Results.Ok("CSV successfully imported!");
                }
            });

    }
}