using System.ComponentModel.DataAnnotations;
using PlatinumCredentialManager.Api.Dtos.Credential;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class ExportAllCategoriesDto
(
    [Required] int Id,
    [Required][StringLength(100)] string CategoryName,
    [Required] ICollection<ExportAllCredentialsDto> Credentials
);