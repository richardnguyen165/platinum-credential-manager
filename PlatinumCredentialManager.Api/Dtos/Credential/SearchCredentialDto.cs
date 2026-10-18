using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class SearchCredentialDto
(
    string? CategoryName,
    string? ServiceName,
    [Required] string CreateUpdateChoice,
    [Required] string UserDateChoice,
    string? StartDate,
    string? EndDate
);