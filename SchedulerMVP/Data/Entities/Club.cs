using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class Club
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    // Note: ApplicationUser is in ApplicationDbContext, so we can't have navigation property here
    public ICollection<Place> Places { get; set; } = new List<Place>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<ScheduleTemplate> ScheduleTemplates { get; set; } = new List<ScheduleTemplate>();
    public ICollection<GroupType> GroupTypes { get; set; } = new List<GroupType>();
}

