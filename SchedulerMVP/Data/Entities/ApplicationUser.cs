using Microsoft.AspNetCore.Identity;

namespace SchedulerMVP.Data.Entities;

public class ApplicationUser : IdentityUser
{
    // Last successful login timestamp (UTC)
    public DateTimeOffset? LastLoginAt { get; set; }
    
    // Club/Förening association (nullable for migration)
    // Note: Club entity is in AppDbContext, so we can't have navigation property here
    public Guid? ClubId { get; set; }
    
    // ONBOARDING COMPLETELY REMOVED - No OnboardingCompletedStep property
    // This ensures login works 100% without any onboarding interference
}

