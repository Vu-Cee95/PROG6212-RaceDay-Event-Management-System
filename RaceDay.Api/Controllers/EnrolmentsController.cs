using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Event enrolments: Participants enter events, Organisers view enrolments for their events.
[ApiController]
[Tags("Enrolments")]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly IEnrolmentService _enrolments;

    public EnrolmentsController(IEnrolmentService enrolments)
    {
        _enrolments = enrolments;
    }

    // Enter an event.
    [HttpPost("api/enrolments")]
    [RequireRole(UserRole.Participant)]
    [SwaggerOperation(Summary = "Enrol the logged-in Participant in an event",
        Description = "Participant only. Body: { eventId, categoryId }. The category must belong to the event. New enrolments start as 'Pending'.")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol([FromBody] CreateEnrolmentRequest request)
    {
        var result = await _enrolments.EnrolAsync(request);
        return result.ToActionResult(this);
    }

    // View a Participant's own enrolments.
    [HttpGet("api/users/{userId:int}/enrolments")]
    [RequireRole(UserRole.Participant)]
    [SwaggerOperation(Summary = "Retrieve a Participant's own enrolments",
        Description = "Participant only. Returns the events the Participant has entered, their chosen category and enrolment status. 403 when requesting another user's enrolments.")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForParticipant(int userId)
    {
        var result = await _enrolments.GetForParticipantAsync(userId);
        return result.ToActionResult(this);
    }

    // View all enrolments for an event.
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Retrieve all enrolments for an event",
        Description = "Organiser only; the caller must own the event. Lists every Participant, their category and status.")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var result = await _enrolments.GetForEventAsync(eventId);
        return result.ToActionResult(this);
    }

    // Update an enrolment's status.
    [HttpPut("api/enrolments/{enrolmentId:int}/status")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Confirm, cancel or reset an enrolment",
        Description = "Organiser only; the caller must own the event. Body: { status } where status is Pending, Confirmed or Cancelled.")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int enrolmentId, [FromBody] UpdateEnrolmentStatusRequest request)
    {
        var result = await _enrolments.UpdateStatusAsync(enrolmentId, request);
        return result.ToActionResult(this);
    }
}
