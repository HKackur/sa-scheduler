using Microsoft.AspNetCore.Identity;

namespace SchedulerMVP.Data.Entities;

public class ApplicationUser : IdentityUser
{
    // Last successful login timestamp (UTC)
    public DateTimeOffset? LastLoginAt { get; set; }
    
    // ONBOARDING COMPLETELY REMOVED - No OnboardingCompletedStep property
    // This ensures login works 100% without any onboarding interference
}

