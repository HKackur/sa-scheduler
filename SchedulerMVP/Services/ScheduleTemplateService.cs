using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using System;

namespace SchedulerMVP.Services;

public class ScheduleTemplateService : IScheduleTemplateService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ScheduleTemplateService> _logger;
    private readonly UserContextService _userContext;

    public ScheduleTemplateService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ScheduleTemplateService> logger, UserContextService userContext)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _userContext = userContext;
    }

    private static bool HasOwnerConflict(string? resourceOwnerId, string? currentUserId, bool isAdmin)
        => !isAdmin
           && !string.IsNullOrEmpty(currentUserId)
           && !string.IsNullOrEmpty(resourceOwnerId)
           && !string.Equals(resourceOwnerId, currentUserId, StringComparison.Ordinal);

    public async Task<List<ScheduleTemplate>> GetTemplatesForPlaceAsync(Guid placeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        var query = db.ScheduleTemplates.Where(t => t.PlaceId == placeId);

        // Admin can see all templates, regular users only their own
        if (!isAdmin && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    // Global accessors (forward-compatible for global templates)
    public async Task<List<ScheduleTemplate>> GetTemplatesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var userId = _userContext.GetCurrentUserId();
        bool isAdmin = false;
        try
        {
            isAdmin = await _userContext.IsAdminAsync();
        }
        catch { /* If admin check fails, treat as non-admin */ }

        var query = db.ScheduleTemplates.AsQueryable();

        // Admin can see all templates, regular users only their own
        if (!isAdmin && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<ScheduleTemplate?> GetByIdAsync(Guid templateId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var template = await db.ScheduleTemplates.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == templateId);
        if (template == null) return null;

        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        // Check access rights
        if (HasOwnerConflict(template.UserId, userId, isAdmin))
        {
            return null; // User doesn't have access
        }

        return template;
    }

    public async Task<ScheduleTemplate> CreateAsync(Guid placeId, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var userId = _userContext.GetCurrentUserId();
        var t = new ScheduleTemplate { Id = Guid.NewGuid(), PlaceId = placeId, Name = name };
        
        // Set UserId for the current user
        if (!string.IsNullOrEmpty(userId))
        {
            t.UserId = userId;
        }

        db.ScheduleTemplates.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    public async Task<ScheduleTemplate> CreateGlobalAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        // For now, create without binding to a specific place by picking any existing place if available.
        // This keeps current schema stable until we migrate PlaceId to be nullable.
        var userId = _userContext.GetCurrentUserId();
        var anyPlaceId = await db.Places.Select(p => p.Id).FirstOrDefaultAsync();
        if (anyPlaceId == Guid.Empty)
        {
            // If no places exist at all, fabricate a GUID to satisfy non-null constraint; bookings will still be empty.
            // Consumers should not depend on Place here for new users without places.
            anyPlaceId = Guid.NewGuid();
        }
        var t = new ScheduleTemplate { Id = Guid.NewGuid(), PlaceId = anyPlaceId, Name = name };
        
        // Set UserId for the current user
        if (!string.IsNullOrEmpty(userId))
        {
            t.UserId = userId;
        }

        db.ScheduleTemplates.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    // Create template for a specific user (used by admin after user creation)
    public async Task<ScheduleTemplate> CreateForUserAsync(string ownerUserId, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var anyPlaceId = await db.Places.Select(p => p.Id).FirstOrDefaultAsync();
        if (anyPlaceId == Guid.Empty)
        {
            anyPlaceId = Guid.NewGuid();
        }
        var t = new ScheduleTemplate { Id = Guid.NewGuid(), PlaceId = anyPlaceId, Name = name, UserId = ownerUserId };
        db.ScheduleTemplates.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    public async Task<ScheduleTemplate> SaveAsNewAsync(Guid sourceTemplateId, string newName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var src = await db.ScheduleTemplates.Include(t => t.Bookings).FirstAsync(t => t.Id == sourceTemplateId);
        
        // Check access to source template
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (HasOwnerConflict(src.UserId, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("You don't have permission to copy this template");
        }

        var clone = new ScheduleTemplate { Id = Guid.NewGuid(), PlaceId = src.PlaceId, Name = newName };
        clone.UserId = userId; // New template belongs to current user
        db.ScheduleTemplates.Add(clone);
        foreach (var b in src.Bookings)
        {
            db.BookingTemplates.Add(new BookingTemplate
            {
                Id = Guid.NewGuid(),
                ScheduleTemplateId = clone.Id,
                AreaId = b.AreaId,
                GroupId = b.GroupId,
                DayOfWeek = b.DayOfWeek,
                StartMin = b.StartMin,
                EndMin = b.EndMin,
                Notes = b.Notes
            });
        }
        await db.SaveChangesAsync();
        return clone;
    }

    public async Task<ScheduleTemplate> UpdateTemplateNameAsync(Guid templateId, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var t = await db.ScheduleTemplates.FirstAsync(x => x.Id == templateId);
        
        // Check access
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (HasOwnerConflict(t.UserId, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("You don't have permission to update this template");
        }

        t.Name = name;
        await db.SaveChangesAsync();
        return t;
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var t = await db.ScheduleTemplates.Include(x => x.Bookings).FirstAsync(x => x.Id == templateId);
        
        // Check access
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (HasOwnerConflict(t.UserId, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this template");
        }

        // Remove any CalendarBookings that were copied from this ScheduleTemplate
        // SourceTemplateId now refers to ScheduleTemplate.Id, not BookingTemplate.Id
        var calendarBookings = await db.CalendarBookings
            .Where(cb => cb.SourceTemplateId.HasValue && cb.SourceTemplateId == templateId)
            .ToListAsync();

        if (calendarBookings.Any())
        {
            db.CalendarBookings.RemoveRange(calendarBookings);
        }
        
        // Then remove the BookingTemplates
        if (t.Bookings.Any())
        {
            db.BookingTemplates.RemoveRange(t.Bookings);
        }
        
        // Finally remove the ScheduleTemplate
        db.ScheduleTemplates.Remove(t);
        await db.SaveChangesAsync();
    }

    public async Task<BookingTemplate> CreateBookingAsync(Guid templateId, Guid areaId, Guid groupId, int dayOfWeek, int startMin, int endMin, string? notes)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var b = new BookingTemplate
        {
            Id = Guid.NewGuid(),
            ScheduleTemplateId = templateId,
            AreaId = areaId,
            GroupId = groupId,
            DayOfWeek = dayOfWeek,
            StartMin = startMin,
            EndMin = endMin,
            Notes = notes
        };
        db.BookingTemplates.Add(b);
        await db.SaveChangesAsync();
        return b;
    }

    public async Task<BookingTemplate> UpdateBookingAsync(Guid bookingId, int? dayOfWeek, int? startMin, int? endMin, Guid? areaId, Guid? groupId, string? notes)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var b = await db.BookingTemplates.Include(bt => bt.ScheduleTemplate).FirstAsync(x => x.Id == bookingId);
        
        // Check access via ScheduleTemplate
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (HasOwnerConflict(b.ScheduleTemplate?.UserId, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("You don't have permission to update this booking");
        }
        if (dayOfWeek.HasValue) b.DayOfWeek = dayOfWeek.Value;
        if (startMin.HasValue) b.StartMin = startMin.Value;
        if (endMin.HasValue) b.EndMin = endMin.Value;
        if (areaId.HasValue) b.AreaId = areaId.Value;
        if (groupId.HasValue) b.GroupId = groupId.Value;
        if (notes is not null) b.Notes = notes;
        await db.SaveChangesAsync();
        return b;
    }

    public async Task DeleteBookingAsync(Guid bookingId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var b = await db.BookingTemplates.Include(bt => bt.ScheduleTemplate).FirstAsync(x => x.Id == bookingId);
        
        // Check access via ScheduleTemplate
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (HasOwnerConflict(b.ScheduleTemplate?.UserId, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this booking");
        }

        // Note: SourceTemplateId now refers to ScheduleTemplate.Id, not BookingTemplate.Id
        // Calendar bookings copied from a BookingTemplate will have SourceTemplateId pointing to the ScheduleTemplate
        // So we cannot directly find calendar bookings for a specific BookingTemplate using SourceTemplateId
        // Calendar bookings will remain until the ScheduleTemplate is deleted
        // This is acceptable since calendar bookings are independent once created

        db.BookingTemplates.Remove(b);
        await db.SaveChangesAsync();
    }

    public async Task<BookingTemplate> DuplicateBookingAsync(Guid bookingId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var src = await db.BookingTemplates.Include(bt => bt.ScheduleTemplate).FirstAsync(x => x.Id == bookingId);
        
        // Check access via ScheduleTemplate
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (HasOwnerConflict(src.ScheduleTemplate?.UserId, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("You don't have permission to duplicate this booking");
        }
        var clone = new BookingTemplate
        {
            Id = Guid.NewGuid(),
            ScheduleTemplateId = src.ScheduleTemplateId,
            AreaId = src.AreaId,
            GroupId = src.GroupId,
            DayOfWeek = src.DayOfWeek,
            StartMin = src.StartMin,
            EndMin = src.EndMin,
            Notes = src.Notes
        };
        db.BookingTemplates.Add(clone);
        await db.SaveChangesAsync();
        return clone;
    }
}


