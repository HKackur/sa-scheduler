using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class UserContextService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDbContextFactory<ApplicationDbContext> _identityDbFactory;
    
    private const string UserIdCacheKey = "UserContextService_UserId";
    private const string IsAdminCacheKey = "UserContextService_IsAdmin";
    private const string ClubIdCacheKey = "UserContextService_ClubId";

    public UserContextService(AuthenticationStateProvider authenticationStateProvider, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IDbContextFactory<ApplicationDbContext> identityDbFactory)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _identityDbFactory = identityDbFactory;
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
        // In Blazor Server, we need to wait for the async task even in a sync method
        // This is safe because GetAuthenticationStateAsync typically completes synchronously
        // or very quickly when the authentication state is already loaded
        try
        {
            var authStateTask = _authenticationStateProvider.GetAuthenticationStateAsync();
            
            // If task is already completed, use the result immediately
            if (authStateTask.IsCompletedSuccessfully)
            {
                var authState = authStateTask.Result;
                var userId = authState?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                    return userId;
            }
            else if (authStateTask.IsCompleted)
            {
                // Task completed but might have faulted
                try
                {
                    var authState = authStateTask.Result;
                    var userId = authState?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                        return userId;
                }
                catch { }
            }
            else
            {
                // Task not completed - in Blazor Server SignalR context, we might need to wait
                // But blocking here could cause deadlocks, so we'll try to get result if possible
                // This is a best-effort approach for sync methods in async contexts
                if (authStateTask.IsCompleted)
                {
                    try
                    {
                        var authState = authStateTask.GetAwaiter().GetResult();
                        var userId = authState?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!string.IsNullOrEmpty(userId))
                            return userId;
                    }
                    catch { }
                }
            }
        }
        catch { }

        return null;
    }

    public async Task<Guid?> GetCurrentUserClubIdAsync()
    {
        // Cache ClubId in HttpContext.Items for request lifetime (performance optimization)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(ClubIdCacheKey, out var cachedClubId) == true)
        {
            return cachedClubId as Guid?;
        }

        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            if (httpContext != null)
                httpContext.Items[ClubIdCacheKey] = null;
            return null;
        }

        try
        {
            // Use IDbContextFactory to avoid concurrency issues (same pattern as ClubService)
            await using var identityDb = await _identityDbFactory.CreateDbContextAsync();
            var user = await identityDb.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            var clubId = user?.ClubId;
            
            // Cache result for request lifetime
            if (httpContext != null)
                httpContext.Items[ClubIdCacheKey] = clubId;
            
            return clubId;
        }
        catch
        {
            if (httpContext != null)
                httpContext.Items[ClubIdCacheKey] = null;
            return null;
        }
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        // Cache userId in HttpContext.Items for request lifetime (performance optimization)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(UserIdCacheKey, out var cachedUserId) == true)
        {
            return cachedUserId as string;
        }

        // Async version that properly awaits AuthenticationStateProvider
        // Use this in async methods for reliable user ID retrieval
        try
        {
            var httpUser = httpContext?.User;
            if (httpUser?.Identity?.IsAuthenticated == true)
            {
                var httpUserId = httpUser.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(httpUserId))
                {
                    // Cache for request lifetime
                    if (httpContext != null)
                        httpContext.Items[UserIdCacheKey] = httpUserId;
                    return httpUserId;
                }
            }

            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var authUserId = authState?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Cache for request lifetime
            if (httpContext != null && !string.IsNullOrEmpty(authUserId))
                httpContext.Items[UserIdCacheKey] = authUserId;
            
            return authUserId;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAdminAsync()
    {
        // Cache isAdmin in HttpContext.Items for request lifetime (performance optimization)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(IsAdminCacheKey, out var cachedIsAdmin) == true)
        {
            return cachedIsAdmin is bool isAdmin && isAdmin;
        }

        try
        {
            // Get userId first (will use cache if available)
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                if (httpContext != null)
                    httpContext.Items[IsAdminCacheKey] = false;
                return false;
            }

            // Check if user is admin (this still requires DB call, but we cache the result)
            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null)
            {
                if (httpContext != null)
                    httpContext.Items[IsAdminCacheKey] = false;
                return false;
            }

            var isAdmin = await _userManager.IsInRoleAsync(appUser, "Admin");
            
            // Cache result for request lifetime
            if (httpContext != null)
                httpContext.Items[IsAdminCacheKey] = isAdmin;
            
            return isAdmin;
        }
        catch
        {
            // If there's any error (e.g., database issue), assume not admin
            if (httpContext != null)
                httpContext.Items[IsAdminCacheKey] = false;
            return false;
        }
    }

    public async Task<bool> CanAccessUserDataAsync(string targetUserId)
    {
        var currentUserId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(currentUserId))
            return false;

        // Admin can access all data
        if (await IsAdminAsync())
            return true;

        // Users can only access their own data
        return currentUserId == targetUserId;
    }
}

