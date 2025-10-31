using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerMVP.Data.Entities;

public class AreaLeaf
{
    [Key, Column(Order = 0)]
    public Guid AreaId { get; set; }
    public Area? Area { get; set; }
    [Key, Column(Order = 1)]
    public Guid LeafId { get; set; }
    public Leaf? Leaf { get; set; }
}


