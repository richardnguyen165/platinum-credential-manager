using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos;

// For creating a credential, sending from the user (PUT)
public record class CreateCredentialDto
(
    [Required][StringLength(100)] string ServiceName,
    [StringLength(100)] string Username, // user may not have a username for application
    [Required][StringLength(100)] string Password
);