namespace RaceDay.Api.Models;

/// <summary>
/// Users table (Part 1 ERD: USERS). Holds both Organisers and Participants,
/// distinguished by the Role column.
/// </summary>
public class User
{
    public int UserId { get; set; }

    /// <summary>NVARCHAR(255), UNIQUE, NOT NULL.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>NVARCHAR(255). Only the hash is stored; never the plain-text password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>NVARCHAR(100), NOT NULL.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>NVARCHAR(20) with CHECK (Role IN ('Organiser','Participant')).</summary>
    public string Role { get; set; } = UserRole.Participant;

    /// <summary>NVARCHAR(500), NULL. Populated by Azure Blob Storage in Part 3.</summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>DATETIME DEFAULT GETDATE().</summary>
    public DateTime DateRegistered { get; set; }

    // Navigation properties
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
