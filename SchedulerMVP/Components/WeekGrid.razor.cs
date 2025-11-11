using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using SchedulerMVP.Services;

namespace SchedulerMVP.Components;

public partial class WeekGrid
{
    [JSInvokable]
    public async Task<bool> CheckBookingConflict(ConflictCheckData data)
    {
        try
        {
            if (data == null)
            {
                return false;
            }

            if (!Guid.TryParse(data.BookingId, out var bookingId))
            {
                return false;
            }

            if (data.BookingType == "calendar")
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var booking = await db.CalendarBookings
                    .Include(b => b.Area)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null || booking.Area == null)
                {
                    return false;
                }

                // For calendar bookings: use the existing booking date (resize doesn't change date, only time)
                // But if a date is provided in data.Date (from data-day attribute), use that
                // Otherwise, fall back to calculating from week start and day index (for drag-and-drop)
                DateOnly newDate;
                if (!string.IsNullOrEmpty(data.Date) && DateOnly.TryParse(data.Date, out var parsedDate))
                {
                    // Use provided date (from data-day attribute in calendar view)
                    newDate = parsedDate;
                }
                else if (data.NewDay > 0)
                {
                    // Fallback: calculate from week start and day index (for drag-and-drop or template view)
                    newDate = UI.CurrentWeekStart.AddDays(data.NewDay - 1);
                }
                else
                {
                    // Last resort: use the booking's existing date
                    newDate = booking.Date;
                }
                
                var conflicts = await ConflictService.CheckCalendarConflictsAsync(
                    booking.Area.Id, 
                    newDate, 
                    data.NewStartMin, 
                    data.NewEndMin, 
                    bookingId);

                return conflicts.Count > 0;
            }
            else if (data.BookingType == "template")
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var template = await db.BookingTemplates
                    .Include(bt => bt.Area)
                    .FirstOrDefaultAsync(bt => bt.Id == bookingId);

                if (template == null || template.Area == null)
                {
                    return false;
                }

                var conflicts = await ConflictService.CheckAreaConflictsAsync(
                    template.Area.Id,
                    data.NewDay,
                    data.NewStartMin,
                    data.NewEndMin,
                    bookingId,
                    template.ScheduleTemplateId);

                return conflicts.Count > 0;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking conflict: {ex.Message}");
            return false;
        }
    }

    [JSInvokable]
    public async Task HandleBookingResize(ResizeData data)
    {
        try
        {
            if (data == null)
            {
                return;
            }

            if (!Guid.TryParse(data.BookingId, out var bookingId))
            {
                return;
            }

            if (data.BookingType == "calendar")
            {
                // Update calendar booking times
                await using var db = await DbFactory.CreateDbContextAsync();
                var booking = await db.CalendarBookings
                    .Include(b => b.Group)
                    .Include(b => b.Area)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return;
                }

                // Update times
                booking.StartMin = data.NewStartMin;
                booking.EndMin = data.NewEndMin;
                booking.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                // Force refresh
                UI.ForceRefresh = true;
                UI.RaiseChanged();
                await LoadDataAsync();
                await InvokeAsync(StateHasChanged);
            }
            else if (data.BookingType == "template")
            {
                // Update booking template times
                await using var db = await DbFactory.CreateDbContextAsync();
                var template = await db.BookingTemplates
                    .Include(bt => bt.ScheduleTemplate)
                    .FirstOrDefaultAsync(bt => bt.Id == bookingId);

                if (template == null)
                {
                    return;
                }

                // Update times
                template.StartMin = data.NewStartMin;
                template.EndMin = data.NewEndMin;
                template.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                // Force refresh
                UI.ForceRefresh = true;
                UI.RaiseChanged();
                await LoadDataAsync();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"HandleBookingResize error: {ex.Message}");
        }
    }
}

public class ResizeData
{
    public string BookingId { get; set; } = string.Empty;
    public string BookingType { get; set; } = string.Empty; // "template" or "calendar"
    public int NewStartMin { get; set; }
    public int NewEndMin { get; set; }
}

