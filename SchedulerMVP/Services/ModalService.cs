using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class ModalService : IModalService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ModalService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Modal>> GetAllModalsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        // Debug: Log which database provider is being used
        var provider = db.Database.ProviderName ?? "unknown";
        Console.WriteLine($"[ModalService] GetAllModalsAsync - Using provider: {provider}");
        
        var modals = await db.Modals
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        
        Console.WriteLine($"[ModalService] GetAllModalsAsync - Found {modals.Count} modals");
        
        return modals;
    }

    public async Task<Modal?> GetModalByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Modals
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Modal> CreateModalAsync(Modal modal)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        modal.Id = Guid.NewGuid();
        modal.CreatedAt = DateTime.UtcNow;
        modal.UpdatedAt = DateTime.UtcNow;
        
        db.Modals.Add(modal);
        await db.SaveChangesAsync();
        
        return modal;
    }

    public async Task<Modal> UpdateModalAsync(Modal modal)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        modal.UpdatedAt = DateTime.UtcNow;
        
        db.Modals.Update(modal);
        await db.SaveChangesAsync();
        
        return modal;
    }

    public async Task DeleteModalAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var modal = await db.Modals.FindAsync(id);
        if (modal != null)
        {
            db.Modals.Remove(modal);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Modal>> GetActiveModalsForUserAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        // Debug: Log which database provider is being used
        var provider = db.Database.ProviderName ?? "unknown";
        Console.WriteLine($"[ModalService] GetActiveModalsForUserAsync - Using provider: {provider}, UserId: {userId}");
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Optimized query: Get active modals that user hasn't read
        // Uses indexes on (StartDate, EndDate) and (ModalId, UserId)
        var activeModals = await db.Modals
            .AsNoTracking()
            .Where(m => m.StartDate <= today && m.EndDate >= today)
            .Where(m => !db.ModalReadBy.Any(mrb => mrb.ModalId == m.Id && mrb.UserId == userId))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
        
        Console.WriteLine($"[ModalService] GetActiveModalsForUserAsync - Found {activeModals.Count} active modals for user");
        
        return activeModals;
    }

    public async Task MarkModalAsReadAsync(Guid modalId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        // Check if already marked as read
        var existing = await db.ModalReadBy
            .FirstOrDefaultAsync(mrb => mrb.ModalId == modalId && mrb.UserId == userId);
        
        if (existing == null)
        {
            var readBy = new ModalReadBy
            {
                Id = Guid.NewGuid(),
                ModalId = modalId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            };
            
            db.ModalReadBy.Add(readBy);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetReadCountAsync(Guid modalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ModalReadBy
            .AsNoTracking()
            .CountAsync(mrb => mrb.ModalId == modalId);
    }
}

