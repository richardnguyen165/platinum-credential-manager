using System.ComponentModel.DataAnnotations;

public record class UpdateCredentialDto
{
    // Not necessary to update a field
    [StringLength(100)] string ServiceName;
    [StringLength(100)] string? Username; // user may not have a username for application
    [StringLength(100)] string Password;
}