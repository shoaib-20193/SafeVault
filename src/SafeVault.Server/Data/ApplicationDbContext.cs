using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafeVault.Server.Models;

namespace SafeVault.Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<VaultRecord> VaultRecords => Set<VaultRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<VaultRecord>()
            .HasOne(record => record.Owner)
            .WithMany()
            .HasForeignKey(record => record.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}