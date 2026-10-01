namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class SearchCredentialResultsDto
(
    string CategoryName,
    string ServiceName,
    DateOnly DateCreated,
    DateOnly DateLastUpdated
);