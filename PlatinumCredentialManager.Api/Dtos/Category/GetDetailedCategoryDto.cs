// Alias for the credential model
using System.ComponentModel.DataAnnotations;
using PlatinumCredentialManager.Api.Dtos.Credential;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class GetDetailedCategoryDto
(
    [Required] int Id,
    [Required][StringLength(100)] string CategoryName,
    [Required] ICollection<GetAllCredentialsDto> Credentials
);