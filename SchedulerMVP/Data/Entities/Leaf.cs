using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class Leaf
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid PlaceId { get; set; }
    public Place? Place { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<AreaLeaf> AreaLeafs { get; set; } = new List<AreaLeaf>();
}


