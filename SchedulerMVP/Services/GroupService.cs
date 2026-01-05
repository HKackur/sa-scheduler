using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

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
        var userId = await _userContext.GetCurrentUserIdAsync();
        var cacheKey = $"groups:{userId ?? "anonymous"}";

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out List<Group>? cachedGroups) && cachedGroups != null)
        {
            return cachedGroups;
        }

        // Load from database
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Groups.AsQueryable();

        // All users (including admin) only see their own groups
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(g => g.UserId == userId);
        }

        var groups = await query
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync();

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

        var userId = await _userContext.GetCurrentUserIdAsync();

        // All users (including admin) can only access their own groups
        if (!string.IsNullOrEmpty(userId) && group.UserId != userId && group.UserId != null)
        {
            return null; // User doesn't have access
        }

        return group;
    }

    public async Task<Group> CreateGroupAsync(Group group)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        
        // Invalidate cache for this user
        var userId = await _userContext.GetCurrentUserIdAsync();
        var cacheKey = $"groups:{userId ?? "anonymous"}";
        _cache.Remove(cacheKey);
        
        return group;
    }

    public async Task<Group> UpdateGroupAsync(Group group)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Groups.Update(group);
        await db.SaveChangesAsync();
        
        // Invalidate cache for this user
        var userId = await _userContext.GetCurrentUserIdAsync();
        var cacheKey = $"groups:{userId ?? "anonymous"}";
        _cache.Remove(cacheKey);
        
        return group;
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FindAsync(groupId);
        if (group != null)
        {
            db.Groups.Remove(group);
            await db.SaveChangesAsync();
            
            // Invalidate cache for this user
            var userId = await _userContext.GetCurrentUserIdAsync();
            var cacheKey = $"groups:{userId ?? "anonymous"}";
            _cache.Remove(cacheKey);
        }
    }
}

