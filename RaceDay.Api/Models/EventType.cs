namespace RaceDay.Api.Models;

// EventTypes lookup table (Part 1 ERD: EVENT TYPES). Seeded with Run, Walk and Cycle.
public class EventType
{
    public int EventTypeId { get; set; }

    // NVARCHAR(50), UNIQUE, NOT NULL.
    public string TypeName { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
