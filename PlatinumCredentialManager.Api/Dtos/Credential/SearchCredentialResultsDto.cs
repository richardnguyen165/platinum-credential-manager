namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class SearchCredentialResultsDto
(
    int CredentialId,
    int CategoryId,
    string CategoryName,
    string ServiceName,
    DateOnly DateCreated,
    DateOnly DateLastUpdated
);