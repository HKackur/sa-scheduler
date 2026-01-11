using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class ScheduleTemplate
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid PlaceId { get; set; }
    public Place? Place { get; set; }

    // Multi-tenant: UserId for audit/spårning (behålls för att visa vem som skapade)
    public string? UserId { get; set; }
    
    // Club/Förening association (nullable for migration) - används för access control/isolation
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }

    public ICollection<BookingTemplate> Bookings { get; set; } = new List<BookingTemplate>();
    
    // Navigation property for calendar bookings copied from this template
    public ICollection<CalendarBooking> CalendarBookings { get; set; } = new List<CalendarBooking>();
}


