using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Browse events (public) and manage events (Organiser only).
[ApiController]
[Route("api/events")]
[Tags("Events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;

    public EventsController(IEventService events)
    {
        _events = events;
    }

    // List all events.
    [HttpGet]
    [SwaggerOperation(Summary = "Retrieve all available events",
        Description = "Public. Both roles (and anonymous visitors) can browse events.")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await _events.GetAllAsync());

    // List the events created by the logged-in Organiser.
    [HttpGet("mine")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Retrieve the events created by the logged-in Organiser",
        Description = "Organiser only. Supporting endpoint for the Organiser dashboard.")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine() => Ok(await _events.GetMineAsync());

    // Get a single event.
    [HttpGet("{eventId:int}")]
    [SwaggerOperation(Summary = "Return details of a specific event",
        Description = "Public. Returns 404 if the event does not exist.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int eventId)
    {
        var result = await _events.GetByIdAsync(eventId);
        return result.ToActionResult(this);
    }

    // Create an event.
    [HttpPost]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Create a new RaceDay event",
        Description = "Organiser only. Body: { name, description, eventDate, location, distance, eventTypeId }.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var result = await _events.CreateAsync(request);
        return result.ToActionResult(this);
    }

    // Update an event.
    [HttpPut("{eventId:int}")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Update an existing event",
        Description = "Organiser only. The caller must own the event. Body: { name, description, eventDate, location, distance, eventTypeId }.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int eventId, [FromBody] UpdateEventRequest request)
    {
        var result = await _events.UpdateAsync(eventId, request);
        return result.ToActionResult(this);
    }

    // Delete an event.
    [HttpDelete("{eventId:int}")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Delete an event",
        Description = "Organiser only. The caller must own the event. Deletes the event and its categories; returns 409 if participants have already enrolled.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int eventId)
    {
        var result = await _events.DeleteAsync(eventId);
        return result.ToActionResult(this);
    }
}
