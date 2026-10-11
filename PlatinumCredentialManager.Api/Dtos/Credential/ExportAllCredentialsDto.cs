using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class ExportAllCredentialsDto
(
    [Required] int Id,
    [Required][StringLength(100)] string ServiceName,
    [StringLength(100)] string? Username,
    [Required][StringLength(100)] string Password,
    [Required] DateOnly DateCreated,
    [Required] DateOnly DateLastUpdated
);