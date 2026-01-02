using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerMVP.Data.Entities;

public class ModalReadBy
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid ModalId { get; set; }
    
    [Required]
    [MaxLength(450)] // Identity user ID max length
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    [ForeignKey(nameof(ModalId))]
    public Modal Modal { get; set; } = null!;
}

