using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class Modal
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty; // HTML/richtext content
    
    [Required]
    public DateOnly StartDate { get; set; }
    
    [Required]
    public DateOnly EndDate { get; set; }
    
    [MaxLength(200)]
    public string? LinkRoute { get; set; } // Route to link to (e.g., "/hjalp")
    
    [MaxLength(50)]
    public string? ButtonText { get; set; } // Button text (default "Visa mig")
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<ModalReadBy> ReadBy { get; set; } = new List<ModalReadBy>();
}

