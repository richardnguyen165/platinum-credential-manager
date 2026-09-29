namespace PlatinumCredentialManager.Api.Models;
using Microsoft.EntityFrameworkCore;

// Used to ensure no duplicate users due to vue's refresh behaviour
[Index(nameof(KeycloakId), IsUnique = true)]
public class User
{
    public int Id { get; set; }
    public required string KeycloakId { get; set; }  // the sub claim from the JWT
    public ICollection<Category> Categories { get; set; } = [];
}