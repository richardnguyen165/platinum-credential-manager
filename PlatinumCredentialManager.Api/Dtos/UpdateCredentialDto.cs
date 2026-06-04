using System.ComponentModel.DataAnnotations;

public record class UpdateCredentialDto
{
    // Not necessary to update a field
    [StringLength(100)] string serviceName;
    [StringLength(100)] string? username; // user may not have a username for application
    [StringLength(100)] string password;
}