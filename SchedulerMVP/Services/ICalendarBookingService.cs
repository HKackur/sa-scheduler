using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services
{
    public interface ICalendarBookingService
    {
        Task<List<CalendarBooking>> GetBookingsForWeekAsync(DateOnly weekStart);
        Task<List<CalendarBooking>> GetBookingsForAreaAsync(Guid areaId, DateOnly weekStart);
        Task<CalendarBooking> CreateBookingAsync(CalendarBooking booking);
        Task<CalendarBooking> UpdateBookingAsync(CalendarBooking booking);
        Task DeleteBookingAsync(Guid bookingId);
        Task<List<CalendarBooking>> CopyTemplateToWeekAsync(Guid templateId, DateOnly weekStart);
        Task<List<CalendarBooking>> CopyTemplateToWeekRangeAsync(Guid templateId, DateOnly weekStartInclusive, DateOnly weekEndInclusive);
        Task<List<CalendarBooking>> CopyTemplateToDateRangeAsync(Guid templateId, DateOnly startDateInclusive, DateOnly endDateInclusive);
        Task<bool> HasBookingsForWeekAsync(DateOnly weekStart);
        Task ClearAllCalendarBookingsAsync();
    }
}
