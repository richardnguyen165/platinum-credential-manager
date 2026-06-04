using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos;

// For creating a credential, sending from the user (PUT)
public record class CreateCredentialDto
{
    [Required][StringLength(100)] string serviceName;
    [StringLength(100)] string? username; // user may not have a username for application
    [Required][StringLength(100)] string password;
}