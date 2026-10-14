using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Lookup of event types (Run, Walk, Cycle) used when creating events.
[ApiController]
[Route("api/eventtypes")]
[Tags("Event Types")]
[Produces("application/json")]
public class EventTypesController : ControllerBase
{
    private readonly IEventService _events;

    public EventTypesController(IEventService events)
    {
        _events = events;
    }

    // List event types.
    [HttpGet]
    [SwaggerOperation(Summary = "List all event types (Run, Walk, Cycle)",
        Description = "Public. Supporting lookup so clients know which eventTypeId to send when creating or updating an event.")]
    [ProducesResponseType(typeof(IEnumerable<EventTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await _events.GetEventTypesAsync());
}
