namespace RaceDay.Api.Models;

// Users table (Part 1 ERD: USERS). Holds both Organisers and Participants, distinguished by the Role column.
public class User
{
    public int UserId { get; set; }

    // NVARCHAR(255), UNIQUE, NOT NULL.
    public string Email { get; set; } = string.Empty;

    // NVARCHAR(255). Only the hash is stored; never the plain-text password.
    public string PasswordHash { get; set; } = string.Empty;

    // NVARCHAR(100), NOT NULL.
    public string FullName { get; set; } = string.Empty;

    // NVARCHAR(20) with CHECK (Role IN ('Organiser','Participant')).
    public string Role { get; set; } = UserRole.Participant;

    // NVARCHAR(500), NULL. Populated by Azure Blob Storage in Part 3.
    public string? ProfilePictureUrl { get; set; }

    // DATETIME DEFAULT GETDATE().
    public DateTime DateRegistered { get; set; }

    // Navigation properties
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
