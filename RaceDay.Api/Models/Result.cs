namespace RaceDay.Api.Models;

/// <summary>
/// Results table (Part 1 ERD: RESULTS). One Result per Enrolment (EnrolmentId
/// is UNIQUE), captured by the Organiser after the Event concludes.
/// </summary>
public class Result
{
    public int ResultId { get; set; }

    /// <summary>FK to Enrolments.EnrolmentId, UNIQUE - gives the 1:1 relationship.</summary>
    public int EnrolmentId { get; set; }

    /// <summary>TIME(0), NULL.</summary>
    public TimeSpan? FinishTime { get; set; }

    /// <summary>INT, NULL.</summary>
    public int? FinishingPosition { get; set; }

    /// <summary>NVARCHAR(255), NULL.</summary>
    public string? Notes { get; set; }

    // Navigation property
    public Enrolment Enrolment { get; set; } = null!;
}
