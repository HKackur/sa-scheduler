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
        
        // ONBOARDING DISABLED - Ignore OnboardingCompletedStep column if it exists in database
        // This allows the app to work even if the column exists in the database
        builder.Entity<ApplicationUser>()
            .Ignore(u => u.OnboardingCompletedStep);
    }
}

