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
        
        // Configure ClubId property (EF Core will handle it automatically, but we ensure it's recognized)
        builder.Entity<ApplicationUser>()
            .Property(u => u.ClubId)
            .HasColumnType("TEXT"); // SQLite stores Guid as TEXT
    }
}

