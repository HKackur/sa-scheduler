using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities
{
    public class CalendarBooking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid AreaId { get; set; }
        public Area Area { get; set; } = null!;
        
        [Required]
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;
        
        [Required]
        public DateOnly Date { get; set; }
        
        [Required]
        public int StartMin { get; set; } // Minutes from midnight
        
        [Required]
        public int EndMin { get; set; } // Minutes from midnight
        
        public string? Notes { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        
        // Reference to the ScheduleTemplate this was copied from (optional)
        public Guid? SourceTemplateId { get; set; }
        public ScheduleTemplate? SourceTemplate { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
