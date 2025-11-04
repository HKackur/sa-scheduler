using Microsoft.AspNetCore.Identity;

namespace SchedulerMVP.Data.Entities;

public class ApplicationUser : IdentityUser
{
    // Last successful login timestamp (UTC)
    public DateTimeOffset? LastLoginAt { get; set; }
    
    // Onboarding completion status: null = not started, 0 = intro shown, 1-3 = steps completed, 4 = fully completed
    public int? OnboardingCompletedStep { get; set; }
}

