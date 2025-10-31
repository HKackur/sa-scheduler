using SchedulerMVP.Services.Models;

namespace SchedulerMVP.Services;

public interface IConflictService
{
    Task<List<ConflictDto>> CheckTemplateConflictsAsync(Guid templateId, IEnumerable<BookingCandidate> candidateBookings);
    Task<List<ConflictDto>> CheckAreaConflictsAsync(Guid areaId, int dayOfWeek, int startMin, int endMin, Guid? excludeBookingId = null, Guid? templateId = null);
    Task<List<ConflictDto>> CheckCalendarConflictsAsync(Guid areaId, DateOnly date, int startMin, int endMin, Guid? excludeBookingId = null);
}


