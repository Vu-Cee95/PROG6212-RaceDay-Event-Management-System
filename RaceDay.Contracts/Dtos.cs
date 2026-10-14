using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

// Request and response models used by the API (and later by the MVC app).
// These are separate from the database entity classes.

#region Authentication

// Body for POST /api/auth/register.
public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    // Either "Organiser" or "Participant".
    [Required, MaxLength(20)]
    public string Role { get; set; } = string.Empty;
}

// Body for POST /api/auth/login.
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// Returned after a successful login or registration.
public class AuthResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

#endregion

#region Profile

// Response for GET /api/users/profile.
public class ProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime DateRegistered { get; set; }
}

// Body for PUT /api/users/profile.
public class UpdateProfileRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500), Url]
    public string? ProfilePictureUrl { get; set; }
}

#endregion

#region Event Types

// Lookup value returned by GET /api/eventtypes (Run, Walk, Cycle).
public class EventTypeDto
{
    public int EventTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}

#endregion

#region Events

// Body for POST /api/events.
public class CreateEventRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    // Distance in kilometres (e.g. 10.00, 21.10, 42.20).
    [Range(0.1, 999.99)]
    public decimal Distance { get; set; }

    // Foreign key to the EventTypes lookup (1 = Run, 2 = Walk, 3 = Cycle).
    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }
}

// Body for PUT /api/events/{eventId}.
public class UpdateEventRequest : CreateEventRequest
{
}

// Response shape for an Event.
public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public int EventTypeId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string? BannerImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

#endregion

#region Categories

// Body for POST /api/events/{eventId}/categories and PUT /api/categories/{categoryId}.
public class CategoryRequest
{
    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Description { get; set; }
}

// Response shape for a Category.
public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

#endregion

#region Enrolments

// Body for POST /api/enrolments.
public class CreateEnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

// Body for PUT /api/enrolments/{enrolmentId}/status.
public class UpdateEnrolmentStatusRequest
{
    // One of: Pending, Confirmed, Cancelled.
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

// Response shape for an Enrolment.
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasResult { get; set; }
}

#endregion

#region Results

// Body for POST /api/enrolments/{enrolmentId}/result.
public class CreateResultRequest
{
    // Finish time in hh:mm:ss format, e.g. "03:45:22".
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }
}

// Response shape for a Result, including the race context.
public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? FinishingPosition { get; set; }
    // Total number of recorded finishers for the event (for "47th out of 312").
    public int TotalFinishers { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Common

// Standard error payload returned for 4xx responses.
public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
}

#endregion
