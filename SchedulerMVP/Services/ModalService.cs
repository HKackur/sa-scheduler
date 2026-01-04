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
        // #region agent log
        try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,C", location = "ModalService.cs:18", message = "GetAllModalsAsync entry", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        // #region agent log
        var provider = db.Database.ProviderName ?? "unknown";
        try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "ModalService.cs:22", message = "Database provider", data = new { provider = provider }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        List<Modal> modals;
        try
        {
            // #region agent log
            try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B,C", location = "ModalService.cs:28", message = "Before query Modals", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            
            modals = await db.Modals
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            
            // #region agent log
            try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "ModalService.cs:33", message = "After query Modals", data = new { count = modals.Count, firstId = modals.FirstOrDefault()?.Id.ToString() ?? "null", firstTitle = modals.FirstOrDefault()?.Title ?? "null" }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try { var st = ex.StackTrace ?? ""; System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ModalService.cs:38", message = "Exception in GetAllModalsAsync", data = new { exceptionType = ex.GetType().Name, message = ex.Message, stackTrace = st.Length > 500 ? st.Substring(0, 500) : st }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            throw;
        }
        
        // #region agent log
        try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,C", location = "ModalService.cs:45", message = "GetAllModalsAsync exit", data = new { count = modals.Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
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
        // #region agent log
        try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "E", location = "ModalService.cs:79", message = "GetActiveModalsForUserAsync entry", data = new { userId = userId ?? "null", userIdLength = userId?.Length ?? 0 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var provider = db.Database.ProviderName ?? "unknown";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // #region agent log
        try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,E", location = "ModalService.cs:87", message = "Before query active modals", data = new { provider = provider, today = today.ToString(), userId = userId ?? "null" }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        List<Modal> activeModals;
        try
        {
            // Optimized query: Get active modals that user hasn't read
            // Uses indexes on (StartDate, EndDate) and (ModalId, UserId)
            activeModals = await db.Modals
                .AsNoTracking()
                .Where(m => m.StartDate <= today && m.EndDate >= today)
                .Where(m => !db.ModalReadBy.Any(mrb => mrb.ModalId == m.Id && mrb.UserId == userId))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            
            // #region agent log
            try { System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C,E", location = "ModalService.cs:96", message = "After query active modals", data = new { count = activeModals.Count, firstId = activeModals.FirstOrDefault()?.Id.ToString() ?? "null" }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try { var st = ex.StackTrace ?? ""; System.IO.File.AppendAllText("/Users/henrikkackur/SchedulerMVP/.cursor/debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ModalService.cs:101", message = "Exception in GetActiveModalsForUserAsync", data = new { exceptionType = ex.GetType().Name, message = ex.Message, stackTrace = st.Length > 500 ? st.Substring(0, 500) : st }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            throw;
        }
        
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

