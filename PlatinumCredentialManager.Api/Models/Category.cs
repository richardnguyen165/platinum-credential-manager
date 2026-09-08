namespace PlatinumCredentialManager.Api.Models;

public class Category
{
    public int Id { get; set; }

    public required string CategoryName { get; set; }

    // Foreign key back to the owning user
    public int UserId { get; set; }

    // Reference navigation - access the acutal User object that owns the Category
    public User? User { get; set; }

    // Navigation property - access a categroy's credentials from the Category Object
    public ICollection<Credential> Credentials { get; set; } = [];
}