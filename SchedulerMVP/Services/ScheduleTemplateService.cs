using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class ScheduleTemplateService : IScheduleTemplateService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ScheduleTemplateService> _logger;
    private readonly UserContextService _userContext;

    public ScheduleTemplateService(AppDbContext db, ILogger<ScheduleTemplateService> logger, UserContextService userContext)
    {
        _db = db;
        _logger = logger;
        _userContext = userContext;
    }

    public async Task<List<ScheduleTemplate>> GetTemplatesForPlaceAsync(Guid placeId)
    {
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        var query = _db.ScheduleTemplates.Where(t => t.PlaceId == placeId);

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
        var userId = _userContext.GetCurrentUserId();
        bool isAdmin = false;
        try
        {
            isAdmin = await _userContext.IsAdminAsync();
        }
        catch { /* If admin check fails, treat as non-admin */ }

        var query = _db.ScheduleTemplates.AsQueryable();

        // Admin can see all templates, regular users only their own
        if (!isAdmin && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<ScheduleTemplate?> GetByIdAsync(Guid templateId)
    {
        var template = await _db.ScheduleTemplates.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == templateId);
        if (template == null) return null;

        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        // Check access rights
        if (!isAdmin && !string.IsNullOrEmpty(userId) && template.UserId != userId)
        {
            return null; // User doesn't have access
        }

        return template;
    }

    public async Task<ScheduleTemplate> CreateAsync(Guid placeId, string name)
    {
        var userId = _userContext.GetCurrentUserId();
        var t = new ScheduleTemplate { Id = Guid.NewGuid(), PlaceId = placeId, Name = name };
        
        // Set UserId for the current user
        if (!string.IsNullOrEmpty(userId))
        {
            t.UserId = userId;
        }

        _db.ScheduleTemplates.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    public async Task<ScheduleTemplate> CreateGlobalAsync(string name)
    {
        // For now, create without binding to a specific place by picking any existing place if available.
        // This keeps current schema stable until we migrate PlaceId to be nullable.
        var userId = _userContext.GetCurrentUserId();
        var anyPlaceId = await _db.Places.Select(p => p.Id).FirstOrDefaultAsync();
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

        _db.ScheduleTemplates.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    // Create template for a specific user (used by admin after user creation)
    public async Task<ScheduleTemplate> CreateForUserAsync(string ownerUserId, string name)
    {
        var anyPlaceId = await _db.Places.Select(p => p.Id).FirstOrDefaultAsync();
        if (anyPlaceId == Guid.Empty)
        {
            anyPlaceId = Guid.NewGuid();
        }
        var t = new ScheduleTemplate { Id = Guid.NewGuid(), PlaceId = anyPlaceId, Name = name, UserId = ownerUserId };
        _db.ScheduleTemplates.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    public async Task<ScheduleTemplate> SaveAsNewAsync(Guid sourceTemplateId, string newName)
    {
        var src = await _db.ScheduleTemplates.Include(t => t.Bookings).FirstAsync(t => t.Id == sourceTemplateId);
        
        // Check access to source template
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (!isAdmin && !string.IsNullOrEmpty(userId) && src.UserId != userId && src.UserId != null)
        {
            throw new UnauthorizedAccessException("You don't have permission to copy this template");
        }

        var clone = new ScheduleTemplate { Id = Guid.NewGuid(), PlaceId = src.PlaceId, Name = newName };
        clone.UserId = userId; // New template belongs to current user
        _db.ScheduleTemplates.Add(clone);
        foreach (var b in src.Bookings)
        {
            _db.BookingTemplates.Add(new BookingTemplate
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
        await _db.SaveChangesAsync();
        return clone;
    }

    public async Task<ScheduleTemplate> UpdateTemplateNameAsync(Guid templateId, string name)
    {
        var t = await _db.ScheduleTemplates.FirstAsync(x => x.Id == templateId);
        
        // Check access
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (!isAdmin && !string.IsNullOrEmpty(userId) && t.UserId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this template");
        }

        t.Name = name;
        await _db.SaveChangesAsync();
        return t;
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        var t = await _db.ScheduleTemplates.Include(x => x.Bookings).FirstAsync(x => x.Id == templateId);
        
        // Check access
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (!isAdmin && !string.IsNullOrEmpty(userId) && t.UserId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this template");
        }

        // First, remove any CalendarBookings that reference this template's BookingTemplates
        var bookingIds = t.Bookings.Select(b => b.Id).ToList();
        if (bookingIds.Any())
        {
            var calendarBookings = await _db.CalendarBookings
                .Where(cb => cb.SourceTemplateId.HasValue && bookingIds.Contains(cb.SourceTemplateId.Value))
                .ToListAsync();
            
            if (calendarBookings.Any())
            {
                _db.CalendarBookings.RemoveRange(calendarBookings);
            }
        }
        
        // Then remove the BookingTemplates
        if (t.Bookings.Any())
        {
            _db.BookingTemplates.RemoveRange(t.Bookings);
        }
        
        // Finally remove the ScheduleTemplate
        _db.ScheduleTemplates.Remove(t);
        await _db.SaveChangesAsync();
    }

    public async Task<BookingTemplate> CreateBookingAsync(Guid templateId, Guid areaId, Guid groupId, int dayOfWeek, int startMin, int endMin, string? notes)
    {
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
        _db.BookingTemplates.Add(b);
        await _db.SaveChangesAsync();
        return b;
    }

    public async Task<BookingTemplate> UpdateBookingAsync(Guid bookingId, int? dayOfWeek, int? startMin, int? endMin, Guid? areaId, Guid? groupId, string? notes)
    {
        var b = await _db.BookingTemplates.Include(bt => bt.ScheduleTemplate).FirstAsync(x => x.Id == bookingId);
        
        // Check access via ScheduleTemplate
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (!isAdmin && !string.IsNullOrEmpty(userId) && b.ScheduleTemplate?.UserId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this booking");
        }
        if (dayOfWeek.HasValue) b.DayOfWeek = dayOfWeek.Value;
        if (startMin.HasValue) b.StartMin = startMin.Value;
        if (endMin.HasValue) b.EndMin = endMin.Value;
        if (areaId.HasValue) b.AreaId = areaId.Value;
        if (groupId.HasValue) b.GroupId = groupId.Value;
        if (notes is not null) b.Notes = notes;
        await _db.SaveChangesAsync();
        return b;
    }

    public async Task DeleteBookingAsync(Guid bookingId)
    {
        var b = await _db.BookingTemplates.Include(bt => bt.ScheduleTemplate).FirstAsync(x => x.Id == bookingId);
        
        // Check access via ScheduleTemplate
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (!isAdmin && !string.IsNullOrEmpty(userId) && b.ScheduleTemplate?.UserId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this booking");
        }

        // Remove any calendar bookings that reference this template booking first
        var calendarRefs = await _db.CalendarBookings
            .Where(cb => cb.SourceTemplateId.HasValue && cb.SourceTemplateId == bookingId)
            .ToListAsync();
        if (calendarRefs.Count > 0)
        {
            _db.CalendarBookings.RemoveRange(calendarRefs);
        }

        _db.BookingTemplates.Remove(b);
        await _db.SaveChangesAsync();
    }

    public async Task<BookingTemplate> DuplicateBookingAsync(Guid bookingId)
    {
        var src = await _db.BookingTemplates.Include(bt => bt.ScheduleTemplate).FirstAsync(x => x.Id == bookingId);
        
        // Check access via ScheduleTemplate
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();
        
        if (!isAdmin && !string.IsNullOrEmpty(userId) && src.ScheduleTemplate?.UserId != userId)
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
        _db.BookingTemplates.Add(clone);
        await _db.SaveChangesAsync();
        return clone;
    }
}


