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

            Console.WriteLine($"[CalendarBookingService] CopyTemplateToDateRangeAsync: templateId={templateId}, start={startDateInclusive}, end={endDateInclusive}");

            var bookingTemplates = await _context.BookingTemplates
                .Include(bt => bt.Area)
                .Include(bt => bt.Group)
                .Where(bt => bt.ScheduleTemplateId == templateId)
                .ToListAsync();

            Console.WriteLine($"[CalendarBookingService] Found {bookingTemplates.Count} booking templates for template {templateId}");

            if (bookingTemplates.Count == 0)
            {
                throw new InvalidOperationException($"No booking templates found for template {templateId}");
            }

            // Validate that all areas and groups exist
            var areaIds = bookingTemplates.Select(bt => bt.AreaId).Distinct().ToList();
            var groupIds = bookingTemplates.Select(bt => bt.GroupId).Distinct().ToList();

            var existingAreas = await _context.Areas
                .Where(a => areaIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            var existingGroups = await _context.Groups
                .Where(g => groupIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync();

            var missingAreas = areaIds.Except(existingAreas).ToList();
            var missingGroups = groupIds.Except(existingGroups).ToList();

            if (missingAreas.Any())
            {
                throw new InvalidOperationException($"Areas not found: {string.Join(", ", missingAreas)}");
            }

            if (missingGroups.Any())
            {
                throw new InvalidOperationException($"Groups not found: {string.Join(", ", missingGroups)}");
            }

            // Check for existing bookings to avoid duplicates
            var existingBookings = await _context.CalendarBookings
                .Where(cb => cb.Date >= startDateInclusive && cb.Date <= endDateInclusive)
                .Select(cb => new { cb.AreaId, cb.GroupId, cb.Date, cb.StartMin, cb.EndMin })
                .ToListAsync();

            Console.WriteLine($"[CalendarBookingService] Found {existingBookings.Count} existing bookings in date range");

            var created = new List<CalendarBooking>();

            // Iterate each date in span
            for (var date = startDateInclusive; date <= endDateInclusive; date = date.AddDays(1))
            {
                // Måndag = 1 enligt vår modell
                var dayOfWeek1to7 = ((int)date.DayOfWeek == 0) ? 7 : (int)date.DayOfWeek; // Sunday->7

                // Hitta de templatebokningar vars veckodag matchar aktuell dag
                foreach (var t in bookingTemplates)
                {
                    if (t.DayOfWeek == dayOfWeek1to7)
                    {
                        // Validate required fields
                        if (t.AreaId == Guid.Empty)
                        {
                            Console.WriteLine($"[CalendarBookingService] WARNING: BookingTemplate {t.Id} has empty AreaId, skipping");
                            continue;
                        }

                        if (t.GroupId == Guid.Empty)
                        {
                            Console.WriteLine($"[CalendarBookingService] WARNING: BookingTemplate {t.Id} has empty GroupId, skipping");
                            continue;
                        }

                        // Check if a booking already exists for this Area, Group, Date, StartMin, EndMin
                        var isDuplicate = existingBookings.Any(eb => 
                            eb.AreaId == t.AreaId && 
                            eb.GroupId == t.GroupId && 
                            eb.Date == date && 
                            eb.StartMin == t.StartMin && 
                            eb.EndMin == t.EndMin);

                        if (isDuplicate)
                        {
                            Console.WriteLine($"[CalendarBookingService] Skipping duplicate booking for {date}: Area={t.AreaId}, Group={t.GroupId}, Time={t.StartMin}-{t.EndMin}");
                            continue;
                        }

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
                        Console.WriteLine($"[CalendarBookingService] Created booking for {date} (day {dayOfWeek1to7}): Area={t.AreaId}, Group={t.GroupId}, Time={t.StartMin}-{t.EndMin}");
                    }
                }
            }

            Console.WriteLine($"[CalendarBookingService] Total bookings to create: {created.Count}");

            if (created.Count > 0)
            {
                try
                {
                    _context.CalendarBookings.AddRange(created);
                    var saved = await _context.SaveChangesAsync();
                    Console.WriteLine($"[CalendarBookingService] Saved {saved} changes to database");
                }
                catch (DbUpdateException dbEx)
                {
                    var innerMsg = dbEx.InnerException?.Message ?? "No inner exception";
                    Console.WriteLine($"[CalendarBookingService] DbUpdateException: {dbEx.Message}");
                    Console.WriteLine($"[CalendarBookingService] InnerException: {innerMsg}");
                    Console.WriteLine($"[CalendarBookingService] StackTrace: {dbEx.StackTrace}");
                    
                    // Re-throw with more context
                    throw new InvalidOperationException(
                        $"Kunde inte spara bokningar: {dbEx.Message}. Detaljer: {innerMsg}", 
                        dbEx);
                }
                catch (Exception ex)
                {
                    var innerMsg = ex.InnerException?.Message ?? "No inner exception";
                    Console.WriteLine($"[CalendarBookingService] Exception: {ex.Message}");
                    Console.WriteLine($"[CalendarBookingService] InnerException: {innerMsg}");
                    Console.WriteLine($"[CalendarBookingService] StackTrace: {ex.StackTrace}");
                    throw new InvalidOperationException(
                        $"Kunde inte spara bokningar: {ex.Message}. Detaljer: {innerMsg}", 
                        ex);
                }
            }
            else
            {
                Console.WriteLine($"[CalendarBookingService] WARNING: No bookings created. Template may not have bookings matching the date range, or all bookings already exist.");
            }

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
