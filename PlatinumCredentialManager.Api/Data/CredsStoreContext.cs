using PlatinumCredentialManager.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Dynamic;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace PlatinumCredentialManager.Api.Data;

public class CredsStoreContext(DbContextOptions<CredsStoreContext> options): DbContext(options)
{
    // DbSet<T> implements IQueryable<T> => no need when doing a LINQ tree
    public DbSet<Credential> Credentials => Set<Credential>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<User> Users => Set<User>();

    // https://www.nikolatech.net/blogs/value-conversions-in-ef-core
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = new EncryptionHelper();

        modelBuilder
            .Entity<Credential>()
            .Property(c => c.Password) // encrypt password
            .HasConversion(
                v => e.Encrypt(v),
                v => e.Decrypt(v)
            );

        modelBuilder
            .Entity<Credential>()
            .Property(c => c.Username) // encrypt username
            .HasConversion(
                v => e.Encrypt(v),
                v => e.Decrypt(v)
            );
    }
}