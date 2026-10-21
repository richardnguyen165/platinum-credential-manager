using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class ImportCredentialsDto
(
    [property: Required] IFormFile File
);