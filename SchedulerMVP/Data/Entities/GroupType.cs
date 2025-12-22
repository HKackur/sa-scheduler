using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class GroupType
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    // Multi-tenant
    public string? UserId { get; set; }
    
    // Standard visningsfärg för grupptypen (används som default när man skapar nya grupper)
    [MaxLength(50)]
    public string StandardDisplayColor { get; set; } = "Ljusblå";
}



