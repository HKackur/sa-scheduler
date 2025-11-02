using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public interface IGroupTypeService
{
    Task<List<GroupType>> GetTypesAsync();
    Task<GroupType> CreateAsync(string name);
    Task<GroupType?> UpdateAsync(Guid id, string name);
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
        var userId = await _userContext.GetCurrentUserIdAsync();
        var isAdmin = await _userContext.IsAdminAsync();
        var q = db.GroupTypes.AsQueryable();
        if (!isAdmin && !string.IsNullOrEmpty(userId)) q = q.Where(x => x.UserId == userId);
        return await q.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<GroupType> CreateAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var userId = await _userContext.GetCurrentUserIdAsync();
        var gt = new GroupType { Id = Guid.NewGuid(), Name = name, UserId = userId };
        db.GroupTypes.Add(gt);
        await db.SaveChangesAsync();
        return gt;
    }

    public async Task<GroupType?> UpdateAsync(Guid id, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gt = await db.GroupTypes.FindAsync(id);
        if (gt == null) return null;
        gt.Name = name;
        await db.SaveChangesAsync();
        return gt;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gt = await db.GroupTypes.FindAsync(id);
        if (gt == null) return;
        var name = gt.Name;
        db.GroupTypes.Remove(gt);
        // Optional: null out groups using this type
        var groups = await db.Groups.Where(g => g.GroupType == name).ToListAsync();
        foreach (var g in groups) g.GroupType = null;
        await db.SaveChangesAsync();
    }
}



