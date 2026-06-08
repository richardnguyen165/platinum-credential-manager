// Alias for the credential model
using CredentialModel = PlatinumCredentialManager.Api.Models.Credential;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class GetDetailedCategroyDto
(
    string CategoryName,
    ICollection<CredentialModel> Credentials
);