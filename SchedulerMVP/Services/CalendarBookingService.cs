using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services
{
    public class CalendarBookingService : ICalendarBookingService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly UserContextService _userContext;

        public CalendarBookingService(IDbContextFactory<AppDbContext> dbFactory, UserContextService userContext)
        {
            _dbFactory = dbFactory;
            _userContext = userContext;
        }

        public async Task<List<CalendarBooking>> GetBookingsForWeekAsync(DateOnly weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            var clubId = await _userContext.GetCurrentUserClubIdAsync();
            var isAdmin = await _userContext.IsAdminAsync();

            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.CalendarBookings
                .Include(cb => cb.Area)
                    .ThenInclude(a => a.Place)
                .Include(cb => cb.Group)
                .Where(cb => cb.Date >= weekStart && cb.Date <= weekEnd);

            // Admin can see all bookings (no filter)
            if (!isAdmin && clubId.HasValue)
            {
                // Regular users see their club's bookings OR bookings with groups that have null ClubId (backward compatibility during migration)
                query = query.Where(cb => cb.Group != null && (cb.Group.ClubId == clubId.Value || cb.Group.ClubId == null));
            }
            else if (!isAdmin && !clubId.HasValue)
            {
                // User without club sees only bookings with groups that have null ClubId
                query = query.Where(cb => cb.Group != null && cb.Group.ClubId == null);
            }

            return await query
                .AsNoTracking()
                .OrderBy(cb => cb.Date)
                .ThenBy(cb => cb.StartMin)
                .ToListAsync();
        }

        public async Task<List<CalendarBooking>> GetBookingsForAreaAsync(Guid areaId, DateOnly weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            var clubId = await _userContext.GetCurrentUserClubIdAsync();
            var isAdmin = await _userContext.IsAdminAsync();

            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.CalendarBookings
                .Include(cb => cb.Area)
                    .ThenInclude(a => a.Place)
                .Include(cb => cb.Group)
                .Where(cb => cb.AreaId == areaId && cb.Date >= weekStart && cb.Date <= weekEnd);

            // Admin can see all bookings (no filter)
            if (!isAdmin && clubId.HasValue)
            {
                // Regular users see their club's bookings OR bookings with groups that have null ClubId (backward compatibility during migration)
                query = query.Where(cb => cb.Group != null && (cb.Group.ClubId == clubId.Value || cb.Group.ClubId == null));
            }
            else if (!isAdmin && !clubId.HasValue)
            {
                // User without club sees only bookings with groups that have null ClubId
                query = query.Where(cb => cb.Group != null && cb.Group.ClubId == null);
            }

            return await query
                .AsNoTracking()
                .OrderBy(cb => cb.Date)
                .ThenBy(cb => cb.StartMin)
                .ToListAsync();
        }

        public async Task<CalendarBooking> CreateBookingAsync(CalendarBooking booking)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.CalendarBookings.Add(booking);
            await db.SaveChangesAsync();
            return booking;
        }

        public async Task<CalendarBooking> UpdateBookingAsync(CalendarBooking booking)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.CalendarBookings.Update(booking);
            await db.SaveChangesAsync();
            return booking;
        }

        public async Task DeleteBookingAsync(Guid bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var booking = await db.CalendarBookings.FindAsync(bookingId);
            if (booking != null)
            {
                db.CalendarBookings.Remove(booking);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<CalendarBooking>> CopyTemplateToWeekAsync(Guid templateId, DateOnly weekStart)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            // Get all booking templates for the given template
            var bookingTemplates = await db.BookingTemplates
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
                    // SourceTemplateId should refer to ScheduleTemplate, not BookingTemplate
                    SourceTemplateId = templateId
                };

                calendarBookings.Add(calendarBooking);
            }

            // Add all bookings to the database
            db.CalendarBookings.AddRange(calendarBookings);
            await db.SaveChangesAsync();

            return calendarBookings;
        }

        public async Task<List<CalendarBooking>> CopyTemplateToWeekRangeAsync(Guid templateId, DateOnly weekStartInclusive, DateOnly weekEndInclusive)
        {
            if (weekEndInclusive < weekStartInclusive)
            {
                throw new ArgumentException("weekEndInclusive must be >= weekStartInclusive");
            }

            await using var db = await _dbFactory.CreateDbContextAsync();
            // Load template bookings once
            var bookingTemplates = await db.BookingTemplates
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
                        // SourceTemplateId should refer to ScheduleTemplate, not BookingTemplate
                        SourceTemplateId = templateId
                    });
                }
            }

            db.CalendarBookings.AddRange(all);
            await db.SaveChangesAsync();
            return all;
        }

        public async Task<List<CalendarBooking>> CopyTemplateToDateRangeAsync(Guid templateId, DateOnly startDateInclusive, DateOnly endDateInclusive)
        {
            if (endDateInclusive < startDateInclusive)
            {
                throw new ArgumentException("endDateInclusive must be >= startDateInclusive");
            }

            Console.WriteLine($"[CalendarBookingService] CopyTemplateToDateRangeAsync: templateId={templateId}, start={startDateInclusive}, end={endDateInclusive}");

            await using var db = await _dbFactory.CreateDbContextAsync();
            var bookingTemplates = await db.BookingTemplates
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

            var existingAreas = await db.Areas
                .Where(a => areaIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            var existingGroups = await db.Groups
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
            var existingBookings = await db.CalendarBookings
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
                            // SourceTemplateId should refer to ScheduleTemplate, not BookingTemplate
                            SourceTemplateId = templateId,
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
                    db.CalendarBookings.AddRange(created);
                    var saved = await db.SaveChangesAsync();
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
            var clubId = await _userContext.GetCurrentUserClubIdAsync();
            var isAdmin = await _userContext.IsAdminAsync();
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.CalendarBookings
                .Where(cb => cb.Date >= weekStart && cb.Date <= weekEnd);
            
            // Admin can see all bookings (no filter)
            if (!isAdmin && clubId.HasValue)
            {
                query = query.Where(cb => cb.Group != null && cb.Group.ClubId == clubId.Value);
            }
            else if (!isAdmin && !clubId.HasValue)
            {
                // User without club sees nothing
                query = query.Where(cb => false);
            }
            
            return await query.AnyAsync();
        }

        public async Task ClearAllCalendarBookingsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var allCalendarBookings = await db.CalendarBookings.ToListAsync();
            db.CalendarBookings.RemoveRange(allCalendarBookings);
            await db.SaveChangesAsync();
        }
    }
}
