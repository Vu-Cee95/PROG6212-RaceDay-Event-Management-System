using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Race results: Organisers capture finish times and positions, Participants view their history.
[ApiController]
[Tags("Results")]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly IResultService _results;

    public ResultsController(IResultService results)
    {
        _results = results;
    }

    // Capture a Participant's result.
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Capture a Participant's finish time and position",
        Description = "Organiser only; the caller must own the event linked to the enrolment. Body: { finishTime (\"hh:mm:ss\"), finishingPosition, notes }.")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int enrolmentId, [FromBody] CreateResultRequest request)
    {
        var result = await _results.CaptureAsync(enrolmentId, request);
        return result.ToActionResult(this);
    }

    // View a Participant's own results.
    [HttpGet("api/users/{userId:int}/results")]
    [RequireRole(UserRole.Participant)]
    [SwaggerOperation(Summary = "Retrieve a Participant's own race results and history",
        Description = "Participant only. Returns event, date, category, finish time, finishing position and total finishers. 403 when requesting another user's results.")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForParticipant(int userId)
    {
        var result = await _results.GetForParticipantAsync(userId);
        return result.ToActionResult(this);
    }

    // View all results for an event.
    [HttpGet("api/events/{eventId:int}/results")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Retrieve all captured results for an event",
        Description = "Organiser only; the caller must own the event. Supporting endpoint for the Organiser results page.")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var result = await _results.GetForEventAsync(eventId);
        return result.ToActionResult(this);
    }
}
