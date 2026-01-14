using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using System.Text;

namespace SchedulerMVP.Services;

public class GroupService : IGroupService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserContextService _userContext;
    private readonly IMemoryCache _cache;
    private const int CacheTTLSeconds = 60; // Cache for 60 seconds

    public GroupService(IDbContextFactory<AppDbContext> dbFactory, UserContextService userContext, IMemoryCache cache)
    {
        _dbFactory = dbFactory;
        _userContext = userContext;
        _cache = cache;
    }

    public async Task<List<Group>> GetGroupsAsync()
    {
        // #region agent log
        try { var userId = _userContext.GetCurrentUserId(); File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", $"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"GroupService.GetGroupsAsync:entry\",\"message\":\"GetGroupsAsync called\",\"data\":{{\"userId\":\"{userId ?? "null"}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n", Encoding.UTF8); } catch { }
        // #endregion
        
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();
        var cacheKey = $"groups:club:{clubId?.ToString() ?? "null"}:admin:{isAdmin}";

        // #region agent log
        try { File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", $"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"GroupService.GetGroupsAsync:afterContext\",\"message\":\"Context values retrieved\",\"data\":{{\"clubId\":\"{clubId?.ToString() ?? "null"}\",\"isAdmin\":{isAdmin.ToString().ToLower()}}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n", Encoding.UTF8); } catch { }
        // #endregion

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out List<Group>? cachedGroups) && cachedGroups != null)
        {
            // #region agent log
            try { File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", $"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"GroupService.GetGroupsAsync:cacheHit\",\"message\":\"Returning cached groups\",\"data\":{{\"count\":{cachedGroups.Count}}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n", Encoding.UTF8); } catch { }
            // #endregion
            return cachedGroups;
        }

        // Load from database
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Groups.AsQueryable();

        // Admin can see all groups, regular users see ONLY groups with exact ClubId match
        if (!isAdmin && clubId.HasValue)
        {
            // Regular users see ONLY groups with exact ClubId match (no null ClubId data)
            query = query.Where(g => g.ClubId == clubId.Value);
            // #region agent log
            try { File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", $"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"GroupService.GetGroupsAsync:filterApplied\",\"message\":\"Applied ClubId filter for regular user\",\"data\":{{\"filterClubId\":\"{clubId.Value}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n", Encoding.UTF8); } catch { }
            // #endregion
        }
        else if (!isAdmin && !clubId.HasValue)
        {
            // User without club sees NOTHING (security: don't show null ClubId data to avoid data leakage)
            query = query.Where(g => false);
            // #region agent log
            try { File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", $"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"GroupService.GetGroupsAsync:filterApplied\",\"message\":\"Applied empty filter for user without club (security)\",\"data\":{{}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n", Encoding.UTF8); } catch { }
            // #endregion
        }
        else
        {
            // #region agent log
            try { File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", $"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"GroupService.GetGroupsAsync:noFilter\",\"message\":\"No filter applied (admin)\",\"data\":{{\"isAdmin\":true}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n", Encoding.UTF8); } catch { }
            // #endregion
        }

        var groups = await query
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync();

        // #region agent log
        try { File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", $"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"GroupService.GetGroupsAsync:queryResult\",\"message\":\"Query executed\",\"data\":{{\"count\":{groups.Count},\"clubIds\":[{string.Join(",", groups.Take(20).Select(g => $"\"{g.ClubId?.ToString() ?? "null"}\""))}]}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n", Encoding.UTF8); } catch { }
        // #endregion

        // Cache for 60 seconds
        _cache.Set(cacheKey, groups, TimeSpan.FromSeconds(CacheTTLSeconds));

        return groups;
    }

    public async Task<Group?> GetGroupAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var group = await db.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);
        
        if (group == null) return null;

        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();

        // Admin can access all groups, regular users can only access their club's groups
        if (!isAdmin && (!clubId.HasValue || group.ClubId != clubId.Value))
        {
            return null; // User doesn't have access
        }

        return group;
    }

    public async Task<Group> CreateGroupAsync(Group group)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        // Set ClubId for the current user's club
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var userId = await _userContext.GetCurrentUserIdAsync();
        
        if (clubId.HasValue)
        {
            group.ClubId = clubId.Value;
        }
        
        // Set UserId for audit/spårning (behålls för att visa vem som skapade)
        if (!string.IsNullOrEmpty(userId))
        {
            group.UserId = userId;
        }
        
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        
        // Invalidate cache for current user's club
        var isAdmin = await _userContext.IsAdminAsync();
        _cache.Remove($"groups:club:{clubId?.ToString() ?? "null"}:admin:{isAdmin}");
        
        return group;
    }

    public async Task<Group> UpdateGroupAsync(Group group)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        // Reload the entity to check access
        var existingGroup = await db.Groups.FindAsync(group.Id);
        if (existingGroup == null) throw new InvalidOperationException("Group not found");
        
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();

        // Admin can update all groups, regular users can only update their club's groups
        if (!isAdmin && (!clubId.HasValue || existingGroup.ClubId != clubId.Value))
        {
            throw new UnauthorizedAccessException("You don't have permission to update this group");
        }

        // Update properties
        existingGroup.Name = group.Name;
        existingGroup.ColorHex = group.ColorHex;
        existingGroup.GroupType = group.GroupType;
        existingGroup.Source = group.Source;
        existingGroup.DisplayColor = group.DisplayColor;
        
        // UserId behålls för audit/spårning (behålls som det är)
        // ClubId should not be changed via update (it's set when creating)
        
        await db.SaveChangesAsync();
        
        // Invalidate cache for current user's club
        _cache.Remove($"groups:club:{clubId?.ToString() ?? "null"}:admin:{isAdmin}");
        
        return existingGroup;
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FindAsync(groupId);
        if (group != null)
        {
            var clubId = await _userContext.GetCurrentUserClubIdAsync();
            var isAdmin = await _userContext.IsAdminAsync();

            // Admin can delete all groups, regular users can only delete their club's groups
            if (!isAdmin && (!clubId.HasValue || group.ClubId != clubId.Value))
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this group");
            }

            db.Groups.Remove(group);
            await db.SaveChangesAsync();
            
            // Invalidate cache
            _cache.Remove($"groups:club:{clubId?.ToString() ?? "null"}:admin:{isAdmin}");
            _cache.Remove("groups:club:*"); // Invalidate all club caches
        }
    }
}

