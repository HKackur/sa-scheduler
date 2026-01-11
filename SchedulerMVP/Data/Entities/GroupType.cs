using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class GroupType
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    // Multi-tenant: UserId for audit/spårning (behålls för att visa vem som skapade)
    public string? UserId { get; set; }
    
    // Club/Förening association (nullable for migration) - används för access control/isolation
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
    
    // Standard visningsfärg för grupptypen (används som default när man skapar nya grupper)
    [MaxLength(50)]
    public string StandardDisplayColor { get; set; } = "Ljusblå";
}



