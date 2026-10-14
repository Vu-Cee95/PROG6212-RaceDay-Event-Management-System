using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventTypeDto>> GetEventTypesAsync();
    Task<IReadOnlyList<EventDto>> GetAllAsync();
    Task<IReadOnlyList<EventDto>> GetMineAsync();
    Task<ServiceResult<EventDto>> GetByIdAsync(int eventId);
    Task<ServiceResult<EventDto>> CreateAsync(CreateEventRequest request);
    Task<ServiceResult<EventDto>> UpdateAsync(int eventId, UpdateEventRequest request);
    Task<ServiceResult<object>> DeleteAsync(int eventId);
}

/// <summary>
/// Event management. Anyone may read events; only Organisers may create them and
/// only the owning Organiser may update or delete them.
/// </summary>
public class EventService : IEventService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EventService(RaceDayDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<EventTypeDto>> GetEventTypesAsync() =>
        await _db.EventTypes
            .OrderBy(t => t.EventTypeId)
            .Select(t => new EventTypeDto { EventTypeId = t.EventTypeId, TypeName = t.TypeName })
            .ToListAsync();

    public async Task<IReadOnlyList<EventDto>> GetAllAsync() =>
        await Query().OrderBy(e => e.EventDate).Select(Projection).ToListAsync();

    public async Task<IReadOnlyList<EventDto>> GetMineAsync() =>
        await Query()
            .Where(e => e.OrganiserId == _currentUser.UserId)
            .OrderBy(e => e.EventDate)
            .Select(Projection)
            .ToListAsync();

    public async Task<ServiceResult<EventDto>> GetByIdAsync(int eventId)
    {
        var dto = await Query().Where(e => e.EventId == eventId).Select(Projection).SingleOrDefaultAsync();
        return dto is null
            ? ServiceResult<EventDto>.NotFound($"Event {eventId} was not found.")
            : ServiceResult<EventDto>.Ok(dto);
    }

    public async Task<ServiceResult<EventDto>> CreateAsync(CreateEventRequest request)
    {
        var validation = await ValidateAsync(request);
        if (validation is not null)
        {
            return ServiceResult<EventDto>.BadRequest(validation);
        }

        var entity = new Event
        {
            OrganiserId = _currentUser.UserId!.Value,
            EventTypeId = request.EventTypeId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            CreatedDate = DateTime.Now
        };

        _db.Events.Add(entity);
        await _db.SaveChangesAsync();

        var created = await GetByIdAsync(entity.EventId);
        return ServiceResult<EventDto>.Created(created.Value!);
    }

    public async Task<ServiceResult<EventDto>> UpdateAsync(int eventId, UpdateEventRequest request)
    {
        var entity = await _db.Events.FindAsync(eventId);
        if (entity is null)
        {
            return ServiceResult<EventDto>.NotFound($"Event {eventId} was not found.");
        }

        if (entity.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<EventDto>.Forbidden("You can only update events that you created.");
        }

        var validation = await ValidateAsync(request);
        if (validation is not null)
        {
            return ServiceResult<EventDto>.BadRequest(validation);
        }

        entity.EventTypeId = request.EventTypeId;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.EventDate = request.EventDate;
        entity.Location = request.Location.Trim();
        entity.Distance = request.Distance;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(eventId);
    }

    public async Task<ServiceResult<object>> DeleteAsync(int eventId)
    {
        var entity = await _db.Events
            .Include(e => e.Categories)
            .SingleOrDefaultAsync(e => e.EventId == eventId);

        if (entity is null)
        {
            return ServiceResult<object>.NotFound($"Event {eventId} was not found.");
        }

        if (entity.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<object>.Forbidden("You can only delete events that you created.");
        }

        // Foreign keys use NO ACTION (as in the Part 1 script), so an event with
        // participant enrolments cannot be deleted - that would orphan their entries.
        if (await _db.Enrolments.AnyAsync(en => en.EventId == eventId))
        {
            return ServiceResult<object>.Conflict(
                "This event has participant enrolments and cannot be deleted.");
        }

        _db.Categories.RemoveRange(entity.Categories);
        _db.Events.Remove(entity);
        await _db.SaveChangesAsync();

        return ServiceResult<object>.NoContent();
    }

    private async Task<string?> ValidateAsync(CreateEventRequest request)
    {
        if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId))
        {
            return $"EventTypeId {request.EventTypeId} is not valid. Use GET /api/eventtypes to see the available types.";
        }

        return null;
    }

    private IQueryable<Event> Query() =>
        _db.Events.AsNoTracking().Include(e => e.Organiser).Include(e => e.EventType);

    private static readonly System.Linq.Expressions.Expression<Func<Event, EventDto>> Projection = e => new EventDto
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName,
        EventTypeId = e.EventTypeId,
        EventType = e.EventType.TypeName,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        BannerImageUrl = e.BannerImageUrl,
        CreatedDate = e.CreatedDate,
        CategoryCount = e.Categories.Count,
        EnrolmentCount = e.Enrolments.Count
    };
}
