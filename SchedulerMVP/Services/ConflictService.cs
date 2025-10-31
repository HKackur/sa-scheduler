using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using SchedulerMVP.Services.Models;

namespace SchedulerMVP.Services;

public class ConflictService(AppDbContext db, ILogger<ConflictService> _logger) : IConflictService
{
    public async Task<List<ConflictDto>> CheckTemplateConflictsAsync(Guid templateId, IEnumerable<BookingCandidate> candidateBookings)
    {
        var candidates = candidateBookings.ToList();
        var conflicts = new List<ConflictDto>();

        foreach (var candidate in candidates)
        {
            var areaLeafIds = await GetLeafIdsForArea(candidate.AreaId);
            var overlapping = await db.BookingTemplates
                .Include(b => b.Area)
                .Include(b => b.Group)
                .Where(b => b.ScheduleTemplateId == templateId && b.DayOfWeek == candidate.DayOfWeek)
                .Where(b => candidate.StartMin < b.EndMin && b.StartMin < candidate.EndMin)
                .ToListAsync();

            foreach (var other in overlapping)
            {
                var otherLeafIds = await GetLeafIdsForArea(other.AreaId);
                if (areaLeafIds.Overlaps(otherLeafIds))
                {
                    conflicts.Add(new ConflictDto(
                        other.Group?.Name ?? "", other.Area?.Name ?? "", other.DayOfWeek, other.StartMin, other.EndMin));
                }
            }
        }

        return conflicts;
    }

    public async Task<List<ConflictDto>> CheckAreaConflictsAsync(Guid areaId, int dayOfWeek, int startMin, int endMin, Guid? excludeBookingId = null, Guid? templateId = null)
    {
        var leafIds = await GetLeafIdsForArea(areaId);
        var query = db.BookingTemplates
            .Include(b => b.Area)
            .Include(b => b.Group)
            .Where(b => b.DayOfWeek == dayOfWeek)
            .Where(b => startMin < b.EndMin && b.StartMin < endMin);

        // Only check conflicts within the same template
        if (templateId.HasValue)
        {
            query = query.Where(b => b.ScheduleTemplateId == templateId.Value);
        }

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookingId.Value);
        }

        var overlapping = await query.ToListAsync();
        var conflicts = new List<ConflictDto>();

        foreach (var other in overlapping)
        {
            var otherLeafIds = await GetLeafIdsForArea(other.AreaId);
            if (leafIds.Overlaps(otherLeafIds))
            {
                conflicts.Add(new ConflictDto(other.Group?.Name ?? "", other.Area?.Name ?? "", other.DayOfWeek, other.StartMin, other.EndMin));
            }
        }

        return conflicts;
    }

    public async Task<List<ConflictDto>> CheckCalendarConflictsAsync(Guid areaId, DateOnly date, int startMin, int endMin, Guid? excludeBookingId = null)
    {
        // Check ONLY against calendar bookings on the same date
        // Templates should NOT conflict with calendar bookings
        var leafIds = await GetLeafIdsForArea(areaId);

        // Calendar bookings (same date only)
        var calQuery = db.CalendarBookings
            .Include(c => c.Area)
            .Include(c => c.Group)
            .Where(c => c.Date == date)
            .Where(c => startMin < c.EndMin && c.StartMin < endMin);
        if (excludeBookingId.HasValue)
        {
            calQuery = calQuery.Where(c => c.Id != excludeBookingId.Value);
        }
        var calOverlaps = await calQuery.ToListAsync();

        var conflicts = new List<ConflictDto>();
        foreach (var other in calOverlaps)
        {
            var otherLeafIds = await GetLeafIdsForArea(other.AreaId);
            if (leafIds.Overlaps(otherLeafIds))
            {
                conflicts.Add(new ConflictDto(other.Group?.Name ?? "", other.Area?.Name ?? "", (int)date.DayOfWeek switch { 0 => 7, _ => (int)date.DayOfWeek }, other.StartMin, other.EndMin));
            }
        }

        return conflicts;
    }

    private async Task<HashSet<Guid>> GetLeafIdsForArea(Guid areaId)
    {
        var ids = await db.AreaLeafs
            .Where(al => al.AreaId == areaId)
            .Select(al => al.LeafId)
            .ToListAsync();
        return ids.ToHashSet();
    }
}


