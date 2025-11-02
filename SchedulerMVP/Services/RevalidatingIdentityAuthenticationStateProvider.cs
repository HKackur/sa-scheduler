using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchedulerMVP.Data.Entities;
using System.Security.Claims;

namespace SchedulerMVP.Services;

// Revalidating authentication state provider for Blazor Server
// This prevents users from being logged out when circuit reconnects
public class RevalidatingIdentityAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IdentityOptions _options;

    public RevalidatingIdentityAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> optionsAccessor)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _options = optionsAccessor.Value;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30); // Revalidate every 30 minutes

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        // Get the user from the authentication state
        var user = authenticationState.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false; // User is not authenticated
        }

        // Check if user still exists and is valid
        var userIdClaim = user.FindFirstValue(_options.ClaimsIdentity.UserIdClaimType);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return false; // No user ID found
        }

        // Validate user still exists in database
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        try
        {
            var appUser = await userManager.FindByIdAsync(userIdClaim);
            if (appUser == null)
            {
                return false; // User no longer exists
            }

            // Check if user is still allowed to sign in
            if (!await userManager.IsEmailConfirmedAsync(appUser) && _options.SignIn.RequireConfirmedEmail)
            {
                return false; // Email not confirmed
            }

            return true; // User is still valid
        }
        catch
        {
            return false; // Error validating user
        }
    }
}

