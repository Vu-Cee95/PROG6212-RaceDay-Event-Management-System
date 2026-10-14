namespace RaceDay.Api.Models;

/// <summary>
/// Events table (Part 1 ERD: EVENTS). An Event is created by one Organiser
/// and has one EventType (Run, Walk or Cycle).
/// </summary>
public class Event
{
    public int EventId { get; set; }

    /// <summary>FK to Users.UserId - the Organiser who owns this event.</summary>
    public int OrganiserId { get; set; }

    /// <summary>FK to EventTypes.EventTypeId.</summary>
    public int EventTypeId { get; set; }

    /// <summary>NVARCHAR(100), NOT NULL.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>NVARCHAR(MAX), NULL.</summary>
    public string? Description { get; set; }

    /// <summary>DATETIME, NOT NULL.</summary>
    public DateTime EventDate { get; set; }

    /// <summary>NVARCHAR(200), NOT NULL.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>DECIMAL(5,2), NOT NULL. Distance in kilometres.</summary>
    public decimal Distance { get; set; }

    /// <summary>NVARCHAR(500), NULL. Populated by Azure Blob Storage in Part 3.</summary>
    public string? BannerImageUrl { get; set; }

    /// <summary>DATETIME DEFAULT GETDATE().</summary>
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public User Organiser { get; set; } = null!;
    public EventType EventType { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
