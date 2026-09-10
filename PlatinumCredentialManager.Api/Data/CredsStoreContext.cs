using PlatinumCredentialManager.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Dynamic;

namespace PlatinumCredentialManager.Api.Data;

public class CredsStoreContext(DbContextOptions<CredsStoreContext> options): DbContext(options)
{
    public DbSet<Credential> Credentials => Set<Credential>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<User> Users => Set<User>();
}