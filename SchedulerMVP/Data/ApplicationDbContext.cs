using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Identity tables will be configured automatically
        // ONBOARDING COMPLETELY REMOVED - No configuration needed
        
        // Configure ClubId property for both SQLite and PostgreSQL
        // For PostgreSQL, we need value converter since we use TEXT (not UUID) for compatibility
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            builder.Entity<ApplicationUser>()
                .Property(u => u.ClubId)
                .HasColumnType("text")
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString() : null!,
                    v => string.IsNullOrEmpty(v) ? null : Guid.Parse(v));
        }
        else
        {
            // SQLite: TEXT type is fine as-is
            builder.Entity<ApplicationUser>()
                .Property(u => u.ClubId)
                .HasColumnType("TEXT");
        }
    }
}

