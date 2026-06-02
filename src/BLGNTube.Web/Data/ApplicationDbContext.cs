using BLGNTube.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BLGNTube.Web.Data;

/// <summary>
/// Uygulamanın EF Core veritabanı bağlamı. Identity tablolarını ve
/// indirme kayıtlarını içerir (SQLite üzerinde).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DownloadRecord> DownloadRecords => Set<DownloadRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DownloadRecord>(entity =>
        {
            entity.HasIndex(d => d.CreatedAt);
            entity.HasIndex(d => new { d.UserId, d.CreatedAt });
            entity.HasIndex(d => new { d.IpAddress, d.CreatedAt });

            entity.HasOne(d => d.User)
                  .WithMany(u => u.Downloads)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
