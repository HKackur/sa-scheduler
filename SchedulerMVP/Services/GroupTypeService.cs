using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using System;

namespace SchedulerMVP.Services;

public interface IGroupTypeService
{
    Task<List<GroupType>> GetTypesAsync();
    Task<GroupType> CreateAsync(string name, string? standardDisplayColor = null);
    Task<GroupType?> UpdateAsync(Guid id, string name, string? standardDisplayColor = null);
    Task DeleteAsync(Guid id);
}

public class GroupTypeService : IGroupTypeService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserContextService _userContext;

    public GroupTypeService(IDbContextFactory<AppDbContext> dbFactory, UserContextService userContext)
    {
        _dbFactory = dbFactory;
        _userContext = userContext;
    }

    public async Task<List<GroupType>> GetTypesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();
        var q = db.GroupTypes.AsQueryable();
        
        // Admin can see all group types, regular users see only their club's group types
        if (isAdmin)
        {
            // Admin sees all group types
            q = q.Where(x => x.ClubId != null);
        }
        else if (clubId.HasValue)
        {
            // Regular users see only their club's group types
            q = q.Where(x => x.ClubId == clubId.Value);
        }
        else
        {
            // User without club sees nothing (backward compatibility: show group types with null ClubId during migration)
            q = q.Where(x => x.ClubId == null);
        }
        
        return await q.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<GroupType> CreateAsync(string name, string? standardDisplayColor = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var userId = await _userContext.GetCurrentUserIdAsync();
        var gt = new GroupType 
        { 
            Id = Guid.NewGuid(), 
            Name = name,
            StandardDisplayColor = standardDisplayColor ?? "Ljusblå"
        };
        
        // Set ClubId for the current user's club
        if (clubId.HasValue)
        {
            gt.ClubId = clubId.Value;
        }
        
        // Set UserId for audit/spårning (behålls för att visa vem som skapade)
        if (!string.IsNullOrEmpty(userId))
        {
            gt.UserId = userId;
        }
        
        db.GroupTypes.Add(gt);
        await db.SaveChangesAsync();
        return gt;
    }

    public async Task<GroupType?> UpdateAsync(Guid id, string name, string? standardDisplayColor = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gt = await db.GroupTypes.FindAsync(id);
        if (gt == null) return null;
        
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();

        // Admin can update all group types, regular users can only update their club's group types
        if (!isAdmin && (!clubId.HasValue || gt.ClubId != clubId.Value))
        {
            throw new UnauthorizedAccessException("You don't have permission to update this group type");
        }
        
        gt.Name = name;
        if (standardDisplayColor != null)
        {
            gt.StandardDisplayColor = standardDisplayColor;
        }
        
        // UserId behålls för audit/spårning (behålls som det är)
        // ClubId should not be changed via update (it's set when creating)
        
        await db.SaveChangesAsync();
        return gt;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gt = await db.GroupTypes.FindAsync(id);
        if (gt == null) return;
        
        var clubId = await _userContext.GetCurrentUserClubIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();

        // Admin can delete all group types, regular users can only delete their club's group types
        if (!isAdmin && (!clubId.HasValue || gt.ClubId != clubId.Value))
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this group type");
        }
        
        var name = gt.Name;
        db.GroupTypes.Remove(gt);
        // Optional: null out groups using this type
        var groups = await db.Groups.Where(g => g.GroupType == name).ToListAsync();
        foreach (var g in groups) g.GroupType = null;
        await db.SaveChangesAsync();
    }
}



