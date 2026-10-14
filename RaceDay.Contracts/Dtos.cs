using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

// ---------------------------------------------------------------------------
// Data Transfer Objects shared between the RaceDay API (Part 2) and the
// RaceDay MVC client (Part 3). DTOs describe the data moving in and out of
// the API; they are deliberately separate from the EF Core entity models.
// Field names follow the request bodies defined in the Part 1 endpoint plan.
// ---------------------------------------------------------------------------

#region Authentication

/// <summary>Body for POST /api/auth/register.</summary>
public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required, MaxLength(20)]
    public string Role { get; set; } = string.Empty;
}

/// <summary>Body for POST /api/auth/login.</summary>
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Returned after a successful login or registration.</summary>
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

/// <summary>Response for GET /api/users/profile.</summary>
public class ProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime DateRegistered { get; set; }
}

/// <summary>Body for PUT /api/users/profile.</summary>
public class UpdateProfileRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500), Url]
    public string? ProfilePictureUrl { get; set; }
}

#endregion

#region Event Types

/// <summary>Lookup value returned by GET /api/eventtypes (Run, Walk, Cycle).</summary>
public class EventTypeDto
{
    public int EventTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}

#endregion

#region Events

/// <summary>Body for POST /api/events.</summary>
public class CreateEventRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Distance in kilometres (e.g. 10.00, 21.10, 42.20).</summary>
    [Range(0.1, 999.99)]
    public decimal Distance { get; set; }

    /// <summary>Foreign key to the EventTypes lookup (1 = Run, 2 = Walk, 3 = Cycle).</summary>
    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }
}

/// <summary>Body for PUT /api/events/{eventId}.</summary>
public class UpdateEventRequest : CreateEventRequest
{
}

/// <summary>Response shape for an Event.</summary>
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

/// <summary>Body for POST /api/events/{eventId}/categories and PUT /api/categories/{categoryId}.</summary>
public class CategoryRequest
{
    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Description { get; set; }
}

/// <summary>Response shape for a Category.</summary>
public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

#endregion

#region Enrolments

/// <summary>Body for POST /api/enrolments.</summary>
public class CreateEnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

/// <summary>Body for PUT /api/enrolments/{enrolmentId}/status.</summary>
public class UpdateEnrolmentStatusRequest
{
    /// <summary>One of: Pending, Confirmed, Cancelled.</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Response shape for an Enrolment.</summary>
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

/// <summary>Body for POST /api/enrolments/{enrolmentId}/result.</summary>
public class CreateResultRequest
{
    /// <summary>Finish time in hh:mm:ss format, e.g. "03:45:22".</summary>
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }
}

/// <summary>Response shape for a Result, including the race context.</summary>
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
    /// <summary>Total number of recorded finishers for the event (for "47th out of 312").</summary>
    public int TotalFinishers { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Common

/// <summary>Standard error payload returned for 4xx responses.</summary>
public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
}

#endregion
