using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services
{
    public class CalendarBookingService : ICalendarBookingService
    {
        private readonly AppDbContext _context;
        private readonly UserContextService _userContext;

        public CalendarBookingService(AppDbContext context, UserContextService userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task<List<CalendarBooking>> GetBookingsForWeekAsync(DateOnly weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            var userId = _userContext.GetCurrentUserId();
            var isAdmin = await _userContext.IsAdminAsync();

            var query = _context.CalendarBookings
                .Include(cb => cb.Area)
                    .ThenInclude(a => a.Place)
                .Include(cb => cb.Group)
                .Include(cb => cb.SourceTemplate)
                .Where(cb => cb.Date >= weekStart && cb.Date <= weekEnd);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(cb => cb.Group.UserId == userId);
            }

            return await query
                .OrderBy(cb => cb.Date)
                .ThenBy(cb => cb.StartMin)
                .ToListAsync();
        }

        public async Task<List<CalendarBooking>> GetBookingsForAreaAsync(Guid areaId, DateOnly weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            var userId = _userContext.GetCurrentUserId();
            var isAdmin = await _userContext.IsAdminAsync();

            var query = _context.CalendarBookings
                .Include(cb => cb.Area)
                    .ThenInclude(a => a.Place)
                .Include(cb => cb.Group)
                .Include(cb => cb.SourceTemplate)
                .Where(cb => cb.AreaId == areaId && cb.Date >= weekStart && cb.Date <= weekEnd);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(cb => cb.Group.UserId == userId);
            }

            return await query
                .OrderBy(cb => cb.Date)
                .ThenBy(cb => cb.StartMin)
                .ToListAsync();
        }

        public async Task<CalendarBooking> CreateBookingAsync(CalendarBooking booking)
        {
            _context.CalendarBookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<CalendarBooking> UpdateBookingAsync(CalendarBooking booking)
        {
            _context.CalendarBookings.Update(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task DeleteBookingAsync(Guid bookingId)
        {
            var booking = await _context.CalendarBookings.FindAsync(bookingId);
            if (booking != null)
            {
                _context.CalendarBookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<CalendarBooking>> CopyTemplateToWeekAsync(Guid templateId, DateOnly weekStart)
        {
            // Get all booking templates for the given template
            var bookingTemplates = await _context.BookingTemplates
                .Include(bt => bt.Area)
                .Include(bt => bt.Group)
                .Where(bt => bt.ScheduleTemplateId == templateId)
                .ToListAsync();

            var calendarBookings = new List<CalendarBooking>();

            foreach (var template in bookingTemplates)
            {
                // Convert day of week (1-7, Mon=1) to actual date
                var targetDate = weekStart.AddDays(template.DayOfWeek - 1);

                var calendarBooking = new CalendarBooking
                {
                    AreaId = template.AreaId,
                    GroupId = template.GroupId,
                    Date = targetDate,
                    StartMin = template.StartMin,
                    EndMin = template.EndMin,
                    Notes = template.Notes,
                    SourceTemplateId = template.Id
                };

                calendarBookings.Add(calendarBooking);
            }

            // Add all bookings to the database
            _context.CalendarBookings.AddRange(calendarBookings);
            await _context.SaveChangesAsync();

            return calendarBookings;
        }

        public async Task<List<CalendarBooking>> CopyTemplateToWeekRangeAsync(Guid templateId, DateOnly weekStartInclusive, DateOnly weekEndInclusive)
        {
            if (weekEndInclusive < weekStartInclusive)
            {
                throw new ArgumentException("weekEndInclusive must be >= weekStartInclusive");
            }

            // Load template bookings once
            var bookingTemplates = await _context.BookingTemplates
                .Include(bt => bt.Area)
                .Include(bt => bt.Group)
                .Where(bt => bt.ScheduleTemplateId == templateId)
                .ToListAsync();

            var all = new List<CalendarBooking>();

            // Iterate weeks step = 7 days
            for (var ws = weekStartInclusive; ws <= weekEndInclusive; ws = ws.AddDays(7))
            {
                foreach (var template in bookingTemplates)
                {
                    var targetDate = ws.AddDays(template.DayOfWeek - 1);

                    all.Add(new CalendarBooking
                    {
                        AreaId = template.AreaId,
                        GroupId = template.GroupId,
                        Date = targetDate,
                        StartMin = template.StartMin,
                        EndMin = template.EndMin,
                        Notes = template.Notes,
                        SourceTemplateId = template.Id
                    });
                }
            }

            _context.CalendarBookings.AddRange(all);
            await _context.SaveChangesAsync();
            return all;
        }

        public async Task<List<CalendarBooking>> CopyTemplateToDateRangeAsync(Guid templateId, DateOnly startDateInclusive, DateOnly endDateInclusive)
        {
            if (endDateInclusive < startDateInclusive)
            {
                throw new ArgumentException("endDateInclusive must be >= startDateInclusive");
            }

            var bookingTemplates = await _context.BookingTemplates
                .Include(bt => bt.Area)
                .Include(bt => bt.Group)
                .Where(bt => bt.ScheduleTemplateId == templateId)
                .ToListAsync();

            var created = new List<CalendarBooking>();

            // Iterate each date in span; for effektivitet, beräkna vecka-start för varje dag vi behöver
            for (var date = startDateInclusive; date <= endDateInclusive; date = date.AddDays(1))
            {
                // Måndag = 1 enligt vår modell
                var dayOfWeek1to7 = ((int)date.DayOfWeek == 0) ? 7 : (int)date.DayOfWeek; // Sunday->7

                // Hitta de templatebokningar vars veckodag matchar aktuell dag
                foreach (var t in bookingTemplates)
                {
                    if (t.DayOfWeek == dayOfWeek1to7)
                    {
                        var booking = new CalendarBooking
                        {
                            Id = Guid.NewGuid(),
                            AreaId = t.AreaId,
                            GroupId = t.GroupId,
                            Date = date,
                            StartMin = t.StartMin,
                            EndMin = t.EndMin,
                            Notes = t.Notes,
                            SourceTemplateId = t.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        created.Add(booking);
                    }
                }
            }

            _context.CalendarBookings.AddRange(created);
            await _context.SaveChangesAsync();
            return created;
        }

        public async Task<bool> HasBookingsForWeekAsync(DateOnly weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            
            return await _context.CalendarBookings
                .AnyAsync(cb => cb.Date >= weekStart && cb.Date <= weekEnd);
        }

        public async Task ClearAllCalendarBookingsAsync()
        {
            var allCalendarBookings = await _context.CalendarBookings.ToListAsync();
            _context.CalendarBookings.RemoveRange(allCalendarBookings);
            await _context.SaveChangesAsync();
        }
    }
}
