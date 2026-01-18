using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using System.Security.Cryptography;

namespace SchedulerMVP.Services;

public class SharedScheduleService : ISharedScheduleService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<SharedScheduleService> _logger;

    public SharedScheduleService(IDbContextFactory<AppDbContext> dbFactory, ILogger<SharedScheduleService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<SharedScheduleLink?> GetByTokenAsync(string token)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var link = await db.SharedScheduleLinks
                .Include(ssl => ssl.ScheduleTemplate)
                    .ThenInclude(st => st!.Place) // Include Place for ScheduleTemplate
                .Include(ssl => ssl.ScheduleTemplate)
                    .ThenInclude(st => st!.Club) // Include Club for ScheduleTemplate
                .Include(ssl => ssl.ScheduleTemplate)
                    .ThenInclude(st => st!.Bookings) // Include Bookings for ScheduleTemplate
                        .ThenInclude(bt => bt.Area) // Include Area for each BookingTemplate
                            .ThenInclude(a => a.Place) // Include Place for each Area
                .Include(ssl => ssl.ScheduleTemplate)
                    .ThenInclude(st => st!.Bookings)
                        .ThenInclude(bt => bt.Group) // Include Group for each BookingTemplate
                .FirstOrDefaultAsync(ssl => ssl.ShareToken == token);

            // Update LastAccessedAt if link exists and is active
            if (link != null && link.IsActive)
            {
                link.LastAccessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return link;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared schedule link by token");
            return null;
        }
    }

    public async Task<SharedScheduleLink?> GetByTemplateIdAsync(Guid templateId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.SharedScheduleLinks
                .Include(ssl => ssl.ScheduleTemplate)
                .FirstOrDefaultAsync(ssl => ssl.ScheduleTemplateId == templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting shared schedule link by template ID {templateId}: {ex.Message}");
            _logger.LogError(ex, $"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<SharedScheduleLink> CreateOrUpdateAsync(Guid templateId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            // Check if table exists - if not, migration hasn't run yet
            try
            {
                var testQuery = await db.SharedScheduleLinks.FirstOrDefaultAsync();
                // If we get here, table exists (even if empty)
            }
            catch (Exception tableEx)
            {
                _logger.LogError(tableEx, $"SharedScheduleLinks table does not exist yet. Migration may not have run. Error: {tableEx.Message}");
                throw new InvalidOperationException("Database table 'SharedScheduleLinks' does not exist. Please wait for migrations to complete or restart the server.", tableEx);
            }
            
            var existingLink = await db.SharedScheduleLinks
                .FirstOrDefaultAsync(ssl => ssl.ScheduleTemplateId == templateId);

            if (existingLink != null)
            {
                // Link already exists, return it
                return existingLink;
            }

            // Create new link
            var newLink = new SharedScheduleLink
            {
                Id = Guid.NewGuid(),
                ScheduleTemplateId = templateId,
                ShareToken = await GenerateShareTokenAsync(),
                IsActive = true,
                AllowWeekView = true,  // Default
                AllowDayView = false,  // Default
                AllowListView = true,  // Default
                CreatedAt = DateTime.UtcNow,
                AllowBookingRequests = false
            };

            db.SharedScheduleLinks.Add(newLink);
            await db.SaveChangesAsync();

            _logger.LogInformation($"Created new shared schedule link for template {templateId} with token {newLink.ShareToken.Substring(0, Math.Min(10, newLink.ShareToken.Length))}...");
            return newLink;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating shared schedule link for template {templateId}: {ex.Message}");
            _logger.LogError(ex, $"Stack trace: {ex.StackTrace}");
            throw; // Re-throw to let caller handle
        }
    }

    public async Task<SharedScheduleLink> UpdateViewSettingsAsync(Guid templateId, bool allowWeekView, bool allowDayView, bool allowListView)
    {
        // Validate: at least one view must be enabled
        if (!allowWeekView && !allowDayView && !allowListView)
        {
            throw new InvalidOperationException("At least one view must be enabled");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var link = await db.SharedScheduleLinks
            .FirstOrDefaultAsync(ssl => ssl.ScheduleTemplateId == templateId);

        if (link == null)
        {
            // Create new link if it doesn't exist
            link = await CreateOrUpdateAsync(templateId);
        }

        link.AllowWeekView = allowWeekView;
        link.AllowDayView = allowDayView;
        link.AllowListView = allowListView;

        await db.SaveChangesAsync();

        return link;
    }

    public async Task ToggleActiveAsync(Guid templateId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var link = await db.SharedScheduleLinks
            .FirstOrDefaultAsync(ssl => ssl.ScheduleTemplateId == templateId);

        if (link == null)
        {
            // Create new link if it doesn't exist
            link = await CreateOrUpdateAsync(templateId);
        }
        else
        {
            link.IsActive = !link.IsActive;
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid templateId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var link = await db.SharedScheduleLinks
            .FirstOrDefaultAsync(ssl => ssl.ScheduleTemplateId == templateId);

        if (link != null)
        {
            db.SharedScheduleLinks.Remove(link);
            await db.SaveChangesAsync();
        }
    }

    public Task<string> GenerateShareTokenAsync()
    {
        // Use cryptographically secure RNG
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256 bits
        rng.GetBytes(bytes);
        
        // Convert to Base64URL (URL-safe Base64)
        // Base64URL uses - and _ instead of + and /, and no padding
        var base64 = Convert.ToBase64String(bytes);
        var base64Url = base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('='); // Remove padding
        
        return Task.FromResult(base64Url);
    }
}
