using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

// PUT = full replace, so every field must be supplied.
public record class UpdateCredentialDto
(
    [Required][StringLength(100)] string ServiceName,
    [StringLength(100)] string Username,
    [Required][StringLength(100)] string Password,
    [Required] int CategoryId
);