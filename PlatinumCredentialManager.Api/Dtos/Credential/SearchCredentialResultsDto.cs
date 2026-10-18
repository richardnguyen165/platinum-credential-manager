using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class SearchCredentialResultsDto
(
    [Required] int CredentialId,
    [Required] int CategoryId,
    [Required] string CategoryName,
    [Required] string ServiceName,
    [Required] DateOnly DateCreated,
    [Required] DateOnly DateLastUpdated
);