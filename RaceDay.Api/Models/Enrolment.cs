namespace RaceDay.Api.Models;

// Enrolments table (Part 1 ERD: ENROLMENT). Records the link between a Participant, the Event they entered and the Category they selected.
public class Enrolment
{
    public int EnrolmentId { get; set; }

    // FK to Users.UserId - the Participant who entered.
    public int ParticipantId { get; set; }

    // FK to Events.EventId.
    public int EventId { get; set; }

    // FK to Categories.CategoryId.
    public int CategoryId { get; set; }

    // DATETIME DEFAULT GETDATE().
    public DateTime EnrolmentDate { get; set; }

    // NVARCHAR(20), NOT NULL, DEFAULT 'Pending'.
    public string Status { get; set; } = EnrolmentStatus.Pending;

    // Navigation properties
    public User Participant { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}
