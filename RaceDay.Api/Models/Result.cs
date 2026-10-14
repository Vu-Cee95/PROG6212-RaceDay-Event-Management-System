namespace RaceDay.Api.Models;

// Results table (Part 1 ERD: RESULTS). One Result per Enrolment (EnrolmentId is UNIQUE), captured by the Organiser after the Event concludes.
public class Result
{
    public int ResultId { get; set; }

    // FK to Enrolments.EnrolmentId, UNIQUE - gives the 1:1 relationship.
    public int EnrolmentId { get; set; }

    // TIME(0), NULL.
    public TimeSpan? FinishTime { get; set; }

    // INT, NULL.
    public int? FinishingPosition { get; set; }

    // NVARCHAR(255), NULL.
    public string? Notes { get; set; }

    // Navigation property
    public Enrolment Enrolment { get; set; } = null!;
}
