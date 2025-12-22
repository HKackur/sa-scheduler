using System.ComponentModel.DataAnnotations;

namespace SchedulerMVP.Data.Entities;

public class Group
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [MaxLength(7)]
    public string ColorHex { get; set; } = "#999999";

    // Upcoming from SportAdmin API; optional to keep backwards compatibility
    [MaxLength(80)]
    public string? GroupType { get; set; }

    // Multi-tenant: UserId for data isolation (nullable for backward compatibility)
    public string? UserId { get; set; }
    
    // Source: "Egen" for groups created in app, "Sportadmin" for groups from SportAdmin integration
    [MaxLength(50)]
    public string Source { get; set; } = "Egen";
    
    // DisplayColor: Name of the color scheme (e.g., "Ljusblå", "Mörkblå", etc.)
    [MaxLength(50)]
    public string DisplayColor { get; set; } = "Ljusblå";
}

public static class GroupDisplayColors
{
    // Based on design: background, border, text colors
    public static Dictionary<string, (string Background, string Border, string Text)> Colors = new()
    {
        { "Ljusblå", ("#E3F4FD", "#2597F3", "#1B76D3") },  // Light blue matching app design
        { "Mörkblå", ("#BBDEFB", "#1976D2", "#0D47A1") },
        { "Ljusgrön", ("#E8F5E9", "#4CAF50", "#2E7D32") },
        { "Mörkgrön", ("#C8E6C9", "#2E7D32", "#1B5E20") },
        { "Ljusröd", ("#FFEBEE", "#F44336", "#C62828") },
        { "Mörkröd", ("#FFCDD2", "#C62828", "#B71C1C") },
        { "Ljuslila", ("#F3E5F5", "#9C27B0", "#6A1B9A") },
        { "Mörklila", ("#E1BEE7", "#6A1B9A", "#4A148C") },
        { "Svart", ("#E0E0E0", "#424242", "#212121") }
    };
}


