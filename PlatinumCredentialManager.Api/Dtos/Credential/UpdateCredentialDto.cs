using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

// PUT = full replace, so every field must be supplied.
public record class UpdateCredentialDto
(
    [Required][StringLength(100)] string ServiceName,
    [StringLength(100)] string? Username,
    [Required][StringLength(100)] string Password,
    // [Required] is a no-op on a non-nullable int (an omitted value just binds to 0), so it is left off here.
    int CategoryId
);