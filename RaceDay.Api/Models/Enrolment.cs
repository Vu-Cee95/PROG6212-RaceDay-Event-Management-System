namespace RaceDay.Api.Models;

/// <summary>
/// Enrolments table (Part 1 ERD: ENROLMENT). Records the link between a
/// Participant, the Event they entered and the Category they selected.
/// </summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }

    /// <summary>FK to Users.UserId - the Participant who entered.</summary>
    public int ParticipantId { get; set; }

    /// <summary>FK to Events.EventId.</summary>
    public int EventId { get; set; }

    /// <summary>FK to Categories.CategoryId.</summary>
    public int CategoryId { get; set; }

    /// <summary>DATETIME DEFAULT GETDATE().</summary>
    public DateTime EnrolmentDate { get; set; }

    /// <summary>NVARCHAR(20), NOT NULL, DEFAULT 'Pending'.</summary>
    public string Status { get; set; } = EnrolmentStatus.Pending;

    // Navigation properties
    public User Participant { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}
