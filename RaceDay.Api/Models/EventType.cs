namespace RaceDay.Api.Models;

/// <summary>
/// EventTypes lookup table (Part 1 ERD: EVENT TYPES). Seeded with Run, Walk and Cycle.
/// </summary>
public class EventType
{
    public int EventTypeId { get; set; }

    /// <summary>NVARCHAR(50), UNIQUE, NOT NULL.</summary>
    public string TypeName { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
