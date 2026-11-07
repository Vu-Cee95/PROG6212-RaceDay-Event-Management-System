using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class EventFormVm
{
    public int? EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, Display(Name = "Event date")]
    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14);

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 999.99), Display(Name = "Distance (km)")]
    public decimal Distance { get; set; } = 10;

    [Display(Name = "Event type")]
    public int EventTypeId { get; set; } = 1;

    public string? BannerImageUrl { get; set; }

    public IFormFile? Banner { get; set; }

    public IEnumerable<SelectListItem> EventTypes { get; set; } = [];

    public static EventFormVm FromDto(EventDto dto, IEnumerable<SelectListItem> types) => new()
    {
        EventId = dto.EventId,
        Name = dto.Name,
        Description = dto.Description,
        EventDate = dto.EventDate,
        Location = dto.Location,
        Distance = dto.Distance,
        EventTypeId = dto.EventTypeId,
        BannerImageUrl = dto.BannerImageUrl,
        EventTypes = types
    };

    public CreateEventRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
        EventDate = EventDate,
        Location = Location,
        Distance = Distance,
        EventTypeId = EventTypeId
    };
}

public class CategoryFormVm
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required, MaxLength(50), Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Description { get; set; }
}

public class CaptureResultVm
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    [Required, Display(Name = "Finish time")]
    public TimeSpan FinishTime { get; set; } = new(1, 30, 0);

    [Range(1, int.MaxValue), Display(Name = "Finishing position")]
    public int FinishingPosition { get; set; } = 1;

    [MaxLength(255)]
    public string? Notes { get; set; }
}

public class ProfileVm
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }

    [Required, MaxLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    public IFormFile? Picture { get; set; }
}
