using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class GetAllCredentialsDto
(
    [Required] int Id,
    [Required][StringLength(100)] string ServiceName,
    [Required][StringLength(100)] string Username,
    [Required] DateOnly DateCreated,
    [Required] DateOnly DateLastUpdated
);