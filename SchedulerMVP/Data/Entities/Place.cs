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

    // Multi-tenant: UserId for audit/spårning (behålls för att visa vem som skapade)
    public string? UserId { get; set; }
    
    // Club/Förening association (nullable for migration) - används för access control/isolation
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }

    public ICollection<Area> Areas { get; set; } = new List<Area>();
    public ICollection<Leaf> Leafs { get; set; } = new List<Leaf>();
}


