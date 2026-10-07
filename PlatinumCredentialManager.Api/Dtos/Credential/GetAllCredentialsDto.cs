using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class GetAllCredentialsDto
(
    [Required] int Id,
    [Required][StringLength(100)] string ServiceName,
    [Required][StringLength(100)] string Username,
    [Required] DateOnly DateCreated,
    [Required] DateOnly DateLastUpdated,
    [Required][StringLength(100)] string CategoryName, // we would need to display the category name
    [Required] int CategoryId
);