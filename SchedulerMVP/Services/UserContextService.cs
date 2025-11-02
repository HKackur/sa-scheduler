using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class UserContextService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserContextService(AuthenticationStateProvider authenticationStateProvider, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public string? GetCurrentUserId()
    {
        // In Blazor Server, HttpContext is null after SignalR connection is established
        // Try HttpContext first (works during initial request), then AuthenticationStateProvider
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            var userId = httpUser.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                return userId;
        }

        // Fallback to AuthenticationStateProvider (works in Blazor Server SignalR circuits)
        // Note: This is a synchronous method, so we can only use already-completed tasks
        try
        {
            var authStateTask = _authenticationStateProvider.GetAuthenticationStateAsync();
            if (authStateTask.IsCompletedSuccessfully)
            {
                var authState = authStateTask.Result;
                var userId = authState?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                    return userId;
            }
        }
        catch { }

        return null;
    }

    public async Task<bool> IsAdminAsync()
    {
        try
        {
            // Use AuthenticationStateProvider first (works in Blazor Server)
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState?.User;
            
            // Fallback to HttpContext if AuthenticationStateProvider didn't work
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                user = _httpContextAccessor.HttpContext?.User;
                if (user == null || user.Identity?.IsAuthenticated != true)
                    return false;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
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

