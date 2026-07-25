namespace PlatinumCredentialManager.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string KeycloakId { get; set; }  // the sub claim from the JWT
    public ICollection<Category> Categories { get; set; } = [];
}