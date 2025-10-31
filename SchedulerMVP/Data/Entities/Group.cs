using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class Group
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [MaxLength(7)]
    public string ColorHex { get; set; } = "#999999";

    // Upcoming from SportAdmin API; optional to keep backwards compatibility
    [MaxLength(80)]
    public string? GroupType { get; set; }

    // Multi-tenant: UserId for data isolation (nullable for backward compatibility)
    public string? UserId { get; set; }
}


