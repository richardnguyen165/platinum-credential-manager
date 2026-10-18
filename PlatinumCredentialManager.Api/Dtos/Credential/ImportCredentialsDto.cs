using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class ImportCredentialsDto
(
    [Required] IFormFile File
);