using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerMVP.Data.Entities;

public class Area
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid PlaceId { get; set; }
    public Place? Place { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? ParentAreaId { get; set; }
    public Area? ParentArea { get; set; }

    [Required]
    [MaxLength(512)]
    public string Path { get; set; } = string.Empty;

    // Ghost booking width percentages for each level
    public int Level1WidthPercent { get; set; } = 100; // When viewing this area, how wide should Level1 areas appear
    public int Level2WidthPercent { get; set; } = 100; // When viewing this area, how wide should Level2 areas appear  
    public int Level3WidthPercent { get; set; } = 100; // When viewing this area, how wide should Level3 areas appear

    public ICollection<AreaLeaf> AreaLeafs { get; set; } = new List<AreaLeaf>();
}


