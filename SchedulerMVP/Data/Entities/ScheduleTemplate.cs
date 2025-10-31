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

    // Multi-tenant: UserId for data isolation (nullable for backward compatibility)
    public string? UserId { get; set; }

    public ICollection<BookingTemplate> Bookings { get; set; } = new List<BookingTemplate>();
}


