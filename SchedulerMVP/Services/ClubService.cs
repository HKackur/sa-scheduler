using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class ClubService : IClubService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDbContextFactory<ApplicationDbContext> _identityDbFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserContextService _userContext;
    private readonly IMemoryCache _cache;
    private const int CacheTTLSeconds = 60;

    public ClubService(
        IDbContextFactory<AppDbContext> dbFactory,
        IDbContextFactory<ApplicationDbContext> identityDbFactory,
        UserManager<ApplicationUser> userManager,
        UserContextService userContext,
        IMemoryCache cache)
    {
        _dbFactory = dbFactory;
        _identityDbFactory = identityDbFactory;
        _userManager = userManager;
        _userContext = userContext;
        _cache = cache;
    }

    public async Task<List<Club>> GetClubsAsync()
    {
        // Only admin can see all clubs
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            return new List<Club>();
        }

        var cacheKey = "clubs:all";
        if (_cache.TryGetValue(cacheKey, out List<Club>? cachedClubs) && cachedClubs != null)
        {
            return cachedClubs;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var clubs = await db.Clubs
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        _cache.Set(cacheKey, clubs, TimeSpan.FromSeconds(CacheTTLSeconds));
        return clubs;
    }

    public async Task<Club?> GetClubAsync(Guid id)
    {
        // Allow any authenticated user to get their own club (for display purposes)
        // Admin can see any club
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            // Regular users can only see their own club
            var userClubId = await _userContext.GetCurrentUserClubIdAsync();
            if (!userClubId.HasValue || userClubId.Value != id)
            {
                return null;
            }
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var club = await db.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return club;
    }

    public async Task<Club> CreateClubAsync(Club club)
    {
        // Only admin can create clubs
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only admin can create clubs");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        
        club.Id = Guid.NewGuid();
        club.CreatedAt = DateTime.UtcNow;
        club.UpdatedAt = DateTime.UtcNow;
        
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        // Invalidate cache
        _cache.Remove("clubs:all");

        return club;
    }

    public async Task<Club> UpdateClubAsync(Club club)
    {
        // Only admin can update clubs
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only admin can update clubs");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var existingClub = await db.Clubs.FindAsync(club.Id);
        if (existingClub == null)
        {
            throw new KeyNotFoundException($"Club with id {club.Id} not found");
        }

        existingClub.Name = club.Name;
        existingClub.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync();

        // Invalidate cache
        _cache.Remove("clubs:all");

        return existingClub;
    }

    public async Task DeleteClubAsync(Guid id)
    {
        // Only admin can delete clubs
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only admin can delete clubs");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var club = await db.Clubs.FindAsync(id);
        if (club == null)
        {
            throw new KeyNotFoundException($"Club with id {id} not found");
        }

        // Check if club has users (query via ApplicationDbContext)
        await using var identityDb = await _identityDbFactory.CreateDbContextAsync();
        var usersInClub = await identityDb.Users
            .Where(u => u.ClubId == id)
            .CountAsync();
        
        if (usersInClub > 0)
        {
            throw new InvalidOperationException($"Cannot delete club with {usersInClub} users. Please reassign users first.");
        }

        // Check if club has data (places, groups, etc)
        var hasPlaces = await db.Places.AnyAsync(p => p.ClubId == id);
        var hasGroups = await db.Groups.AnyAsync(g => g.ClubId == id);
        var hasTemplates = await db.ScheduleTemplates.AnyAsync(st => st.ClubId == id);
        var hasGroupTypes = await db.GroupTypes.AnyAsync(gt => gt.ClubId == id);
        
        if (hasPlaces || hasGroups || hasTemplates || hasGroupTypes)
        {
            throw new InvalidOperationException("Cannot delete club with associated data. Please migrate data first.");
        }

        db.Clubs.Remove(club);
        await db.SaveChangesAsync();

        // Invalidate cache
        _cache.Remove("clubs:all");
    }

    public async Task MigrateUserDataToClubAsync(string userId, Guid clubId)
    {
        // Only admin can migrate data
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only admin can migrate user data");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        
        // Use transaction to ensure atomicity
        await using var transaction = await db.Database.BeginTransactionAsync();
        
        try
        {
            // Migrate Places (and their Areas, Leafs, AreaLeafs are automatically handled via CASCADE)
            var places = await db.Places
                .Where(p => p.UserId == userId)
                .ToListAsync();
            
            foreach (var place in places)
            {
                place.ClubId = clubId;
                // UserId behålls för audit/spårning
            }

            // Migrate Groups
            var groups = await db.Groups
                .Where(g => g.UserId == userId)
                .ToListAsync();
            
            foreach (var group in groups)
            {
                group.ClubId = clubId;
                // UserId behålls för audit/spårning
            }

            // Migrate ScheduleTemplates
            var templates = await db.ScheduleTemplates
                .Where(st => st.UserId == userId)
                .ToListAsync();
            
            foreach (var template in templates)
            {
                template.ClubId = clubId;
                // UserId behålls för audit/spårning
            }

            // Migrate GroupTypes
            var groupTypes = await db.GroupTypes
                .Where(gt => gt.UserId == userId)
                .ToListAsync();
            
            foreach (var groupType in groupTypes)
            {
                groupType.ClubId = clubId;
                // UserId behålls för audit/spårning
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AssignUserToClubAsync(string userId, Guid clubId)
    {
        // Only admin can assign users to clubs
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only admin can assign users to clubs");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }

        // Verify club exists
        await using var db = await _dbFactory.CreateDbContextAsync();
        var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId);
        if (!clubExists)
        {
            throw new KeyNotFoundException($"Club with id {clubId} not found");
        }

        // Assign user to club
        user.ClubId = clubId;
        await _userManager.UpdateAsync(user);

        // Migrate user's data to club
        await MigrateUserDataToClubAsync(userId, clubId);
    }

    public async Task<List<ApplicationUser>> GetUsersInClubAsync(Guid clubId)
    {
        // Only admin can see users in clubs
        var isAdmin = await _userContext.IsAdminAsync();
        if (!isAdmin)
        {
            return new List<ApplicationUser>();
        }

        await using var identityDb = await _identityDbFactory.CreateDbContextAsync();
        var users = await identityDb.Users
            .Where(u => u.ClubId == clubId)
            .OrderBy(u => u.Email)
            .ToListAsync();

        return users;
    }
}

