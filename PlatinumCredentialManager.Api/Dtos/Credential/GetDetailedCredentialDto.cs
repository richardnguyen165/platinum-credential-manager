namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class GetDetailedCredentialDto
(
    int Id,
    string ServiceName,
    string Username,
    string Password,
    DateOnly DateCreated,
    DateOnly DateLastUpdated,
    string CategoryName // we would need to display the category name
);