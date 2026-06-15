// Alias for the credential model
using System.ComponentModel.DataAnnotations;
using CredentialModel = PlatinumCredentialManager.Api.Models.Credential;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class GetDetailedCategoryDto
(
    [Required] int Id,
    [Required][StringLength(100)] string CategoryName,
    [Required] ICollection<CredentialModel> Credentials
);