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
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();
        var cacheKey = $"groups:club:{clubId?.ToString() ?? "null"}:admin:{isAdmin}";

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out List<Group>? cachedGroups) && cachedGroups != null)
        {
            return cachedGroups;
        }

        // Load from database
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Groups.AsQueryable();

        // Admin can see all groups, regular users see only their club's groups
        if (!isAdmin && clubId.HasValue)
        {
            // Regular users see ONLY their club's groups (data must be migrated)
            query = query.Where(g => g.ClubId == clubId.Value);
        }
        else if (!isAdmin && !clubId.HasValue)
        {
            // User without club sees only groups with null ClubId (backward compatibility)
            query = query.Where(g => g.ClubId == null);
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
        
        // Invalidate cache
        var isAdmin = await _userContext.IsAdminAsync();
        _cache.Remove($"groups:club:{clubId?.ToString() ?? "null"}:admin:{isAdmin}");
        _cache.Remove("groups:club:*"); // Invalidate all club caches
        
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
        
        // Invalidate cache
        _cache.Remove($"groups:club:{clubId?.ToString() ?? "null"}:admin:{isAdmin}");
        _cache.Remove("groups:club:*"); // Invalidate all club caches
        
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

