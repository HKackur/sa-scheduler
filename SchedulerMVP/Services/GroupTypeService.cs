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
    private readonly AppDbContext _db;
    private readonly UserContextService _userContext;

    public GroupTypeService(AppDbContext db, UserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<List<GroupType>> GetTypesAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        var q = _db.GroupTypes.AsQueryable();
        if (!isAdmin && !string.IsNullOrEmpty(userId)) q = q.Where(x => x.UserId == userId);
        return await q.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<GroupType> CreateAsync(string name)
    {
        var userId = _userContext.GetCurrentUserId();
        var gt = new GroupType { Id = Guid.NewGuid(), Name = name, UserId = userId };
        _db.GroupTypes.Add(gt);
        await _db.SaveChangesAsync();
        return gt;
    }

    public async Task<GroupType?> UpdateAsync(Guid id, string name)
    {
        var gt = await _db.GroupTypes.FindAsync(id);
        if (gt == null) return null;
        gt.Name = name;
        await _db.SaveChangesAsync();
        return gt;
    }

    public async Task DeleteAsync(Guid id)
    {
        var gt = await _db.GroupTypes.FindAsync(id);
        if (gt == null) return;
        _db.GroupTypes.Remove(gt);
        // Optional: null out groups using this type
        var name = gt.Name;
        var groups = await _db.Groups.Where(g => g.GroupType == name).ToListAsync();
        foreach (var g in groups) g.GroupType = null;
        await _db.SaveChangesAsync();
    }
}



