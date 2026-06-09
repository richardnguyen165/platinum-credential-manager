// Alias for the credential model
using CredentialModel = PlatinumCredentialManager.Api.Models.Credential;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class GetDetailedCategoryDto
(
    int Id,
    string CategoryName,
    ICollection<CredentialModel> Credentials
);