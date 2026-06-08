namespace PlatinumCredentialManager.Api.Models;

public class Category
{
    public int Id { get; set; }

    public required string CategoryName { get; set; }

    // Navigation property - access a categroy's credentials from the Category Object

    public ICollection<Credential> Credentials { get; set; } = [];
}