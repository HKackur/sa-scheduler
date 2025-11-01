using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class UserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserContextService(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<bool> IsAdminAsync()
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return false;

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null)
                return false;

            return await _userManager.IsInRoleAsync(appUser, "Admin");
        }
        catch
        {
            // If there's any error (e.g., database issue), assume not admin
            return false;
        }
    }

    public async Task<bool> CanAccessUserDataAsync(string targetUserId)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return false;

        // Admin can access all data
        if (await IsAdminAsync())
            return true;

        // Users can only access their own data
        return currentUserId == targetUserId;
    }
}

