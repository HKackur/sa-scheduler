using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class Place
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int DefaultDurationMin { get; set; } = 90;
    public int SnapMin { get; set; } = 15;
    public int VisibleStartMin { get; set; } = 420;
    public int VisibleEndMin { get; set; } = 1260;

    // Multi-tenant: UserId for data isolation (nullable for backward compatibility)
    public string? UserId { get; set; }

    public ICollection<Area> Areas { get; set; } = new List<Area>();
    public ICollection<Leaf> Leafs { get; set; } = new List<Leaf>();
}


