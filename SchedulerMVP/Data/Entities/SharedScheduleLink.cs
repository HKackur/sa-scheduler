using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerMVP.Data.Entities;

public class SharedScheduleLink
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ScheduleTemplateId { get; set; }
    public ScheduleTemplate? ScheduleTemplate { get; set; }
    
    // Kryptografiskt säker token (32+ tecken) - INTE Guid för säkerhet
    [Required]
    [MaxLength(128)]
    public string ShareToken { get; set; } = string.Empty;
    
    // Index för snabb lookup av token
    public bool IsActive { get; set; } = true;
    
    // Vyer som ska visas på delad sida (en länk per veckomall, admin väljer vyer)
    public bool AllowWeekView { get; set; } = true;  // Veckovy (default)
    public bool AllowDayView { get; set; } = false;  // Dagsvy
    public bool AllowListView { get; set; } = true;  // Listvy (default)
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    
    // För framtida bokningsförfrågningar
    public bool AllowBookingRequests { get; set; } = false;
}
