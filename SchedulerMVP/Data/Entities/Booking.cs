using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class Booking
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid AreaId { get; set; }
    public Area Area { get; set; } = null!;
    
    [Required]
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
    
    [Required]
    public DateTime Date { get; set; }  // Specific date (YYYY-MM-DD)
    
    [Required]
    public int StartMin { get; set; }  // Minutes from midnight
    
    [Required]
    public int EndMin { get; set; }    // Minutes from midnight
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
