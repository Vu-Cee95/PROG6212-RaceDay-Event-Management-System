namespace RaceDay.Api.Models;

/// <summary>
/// Categories table (Part 1 ERD: CATEGORIES). Age or distance categories
/// defined by the Organiser for a specific Event (e.g. Under 20, Senior, 10km).
/// </summary>
public class Category
{
    public int CategoryId { get; set; }

    /// <summary>FK to Events.EventId.</summary>
    public int EventId { get; set; }

    /// <summary>NVARCHAR(50), NOT NULL.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>NVARCHAR(100), NULL.</summary>
    public string? Description { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
