namespace PlatinumCredentialManager.Api.Dtos.Credential;

using System.ComponentModel.DataAnnotations;

public record class GetDetailedCredentialDto
(
    [Required] int Id,
    [Required][StringLength(100)] string ServiceName,
    [Required][StringLength(100)] string Username,
    [Required][StringLength(100)] string Password,
    [Required] DateOnly DateCreated,
    [Required] DateOnly DateLastUpdated,
    [Required][StringLength(100)] string CategoryName // we would need to display the category name
);