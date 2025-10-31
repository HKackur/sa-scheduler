using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class BookingTemplate
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ScheduleTemplateId { get; set; }
    public ScheduleTemplate? ScheduleTemplate { get; set; }

    [Required]
    public Guid AreaId { get; set; }
    public Area? Area { get; set; }

    [Required]
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    // 1..7 (Mon=1)
    [Range(1,7)]
    public int DayOfWeek { get; set; }
    public int StartMin { get; set; }
    public int EndMin { get; set; }

    public string? Notes { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property for calendar bookings copied from this template
    public ICollection<CalendarBooking> CalendarBookings { get; set; } = new List<CalendarBooking>();
}


