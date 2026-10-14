namespace RaceDay.Api.Models;

// Events table (Part 1 ERD: EVENTS). An Event is created by one Organiser and has one EventType (Run, Walk or Cycle).
public class Event
{
    public int EventId { get; set; }

    // FK to Users.UserId - the Organiser who owns this event.
    public int OrganiserId { get; set; }

    // FK to EventTypes.EventTypeId.
    public int EventTypeId { get; set; }

    // NVARCHAR(100), NOT NULL.
    public string Name { get; set; } = string.Empty;

    // NVARCHAR(MAX), NULL.
    public string? Description { get; set; }

    // DATETIME, NOT NULL.
    public DateTime EventDate { get; set; }

    // NVARCHAR(200), NOT NULL.
    public string Location { get; set; } = string.Empty;

    // DECIMAL(5,2), NOT NULL. Distance in kilometres.
    public decimal Distance { get; set; }

    // NVARCHAR(500), NULL. Populated by Azure Blob Storage in Part 3.
    public string? BannerImageUrl { get; set; }

    // DATETIME DEFAULT GETDATE().
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public User Organiser { get; set; } = null!;
    public EventType EventType { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
