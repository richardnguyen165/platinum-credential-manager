using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class SearchCredentialDto
(
    string? CategoryName,
    string? ServiceName,
    [property: Required] string CreateUpdateChoice,
    [property: Required] string UserDateChoice,
    string? StartDate,
    string? EndDate
);