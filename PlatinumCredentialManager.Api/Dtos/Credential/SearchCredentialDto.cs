namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class SearchCredentialDto
(
    string? CategoryName,
    string? ServiceName,
    string CreateUpdateChoice,
    string UserDateChoice,
    string? StartDate,
    string? EndDate
);