namespace PlatinumCredentialManager.Api.Models;

public class Credential
{
    // Note to self, make variables follow PascalCase convention
    
    // If there is no required, it is optional
    public int Id { get; set; }

    public required string ServiceName { get; set; }

    public string Username { get; set; } = "";

    public required string Password { get; set; }

    // Stores when date was created, only you can get, not set, automatically initializes
    public DateOnly DateCreated { get; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly DateLastUpdated { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
