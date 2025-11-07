using Microsoft.AspNetCore.Identity;

namespace SchedulerMVP.Data.Entities;

public class ApplicationUser : IdentityUser
{
    // Last successful login timestamp (UTC)
    public DateTimeOffset? LastLoginAt { get; set; }
    
    // ONBOARDING DISABLED - Commented out to ensure login works 100%
    // Onboarding completion status: null = not started, 0 = intro completed, 1 = step 1 completed, 2 = step 2 completed, 3 = step 3 completed, 4 = fully completed
    // public int? OnboardingCompletedStep { get; set; }
}

