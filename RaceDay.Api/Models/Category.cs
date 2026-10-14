namespace RaceDay.Api.Models;

// Categories table (Part 1 ERD: CATEGORIES).
public class Category
{
    public int CategoryId { get; set; }

    // FK to Events.EventId.
    public int EventId { get; set; }

    // NVARCHAR(50), NOT NULL.
    public string CategoryName { get; set; } = string.Empty;

    // NVARCHAR(100), NULL.
    public string? Description { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
