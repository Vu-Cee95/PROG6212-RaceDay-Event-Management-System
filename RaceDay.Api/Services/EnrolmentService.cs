using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Services;

public interface IEnrolmentService
{
    Task<ServiceResult<EnrolmentDto>> EnrolAsync(CreateEnrolmentRequest request);
    Task<ServiceResult<IReadOnlyList<EnrolmentDto>>> GetForParticipantAsync(int userId);
    Task<ServiceResult<IReadOnlyList<EnrolmentDto>>> GetForEventAsync(int eventId);
    Task<ServiceResult<EnrolmentDto>> UpdateStatusAsync(int enrolmentId, UpdateEnrolmentStatusRequest request);
}

/// <summary>
/// An Enrolment links a Participant to an Event and the Category they selected.
/// Participants create and view their own enrolments; the owning Organiser views
/// (and may change the status of) enrolments for their events.
/// </summary>
public class EnrolmentService : IEnrolmentService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EnrolmentService(RaceDayDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<EnrolmentDto>> EnrolAsync(CreateEnrolmentRequest request)
    {
        var participantId = _currentUser.UserId!.Value;

        var ev = await _db.Events.FindAsync(request.EventId);
        if (ev is null)
        {
            return ServiceResult<EnrolmentDto>.BadRequest($"Event {request.EventId} does not exist.");
        }

        var category = await _db.Categories.FindAsync(request.CategoryId);
        if (category is null || category.EventId != request.EventId)
        {
            return ServiceResult<EnrolmentDto>.BadRequest(
                $"Category {request.CategoryId} does not belong to event {request.EventId}. Use GET /api/events/{request.EventId}/categories.");
        }

        if (await _db.Enrolments.AnyAsync(en => en.ParticipantId == participantId && en.EventId == request.EventId))
        {
            return ServiceResult<EnrolmentDto>.Conflict("You are already enrolled in this event.");
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.Now,
            Status = EnrolmentStatus.Pending
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var dto = await Query().Where(en => en.EnrolmentId == enrolment.EnrolmentId).Select(Projection).SingleAsync();
        return ServiceResult<EnrolmentDto>.Created(dto);
    }

    public async Task<ServiceResult<IReadOnlyList<EnrolmentDto>>> GetForParticipantAsync(int userId)
    {
        // A Participant may only see their own enrolments.
        if (userId != _currentUser.UserId)
        {
            return ServiceResult<IReadOnlyList<EnrolmentDto>>.Forbidden("You can only view your own enrolments.");
        }

        var list = await Query()
            .Where(en => en.ParticipantId == userId)
            .OrderByDescending(en => en.Event.EventDate)
            .Select(Projection)
            .ToListAsync();

        return ServiceResult<IReadOnlyList<EnrolmentDto>>.Ok(list);
    }

    public async Task<ServiceResult<IReadOnlyList<EnrolmentDto>>> GetForEventAsync(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null)
        {
            return ServiceResult<IReadOnlyList<EnrolmentDto>>.NotFound($"Event {eventId} was not found.");
        }

        if (ev.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<IReadOnlyList<EnrolmentDto>>.Forbidden("You can only view enrolments for events that you created.");
        }

        var list = await Query()
            .Where(en => en.EventId == eventId)
            .OrderBy(en => en.EnrolmentDate)
            .Select(Projection)
            .ToListAsync();

        return ServiceResult<IReadOnlyList<EnrolmentDto>>.Ok(list);
    }

    public async Task<ServiceResult<EnrolmentDto>> UpdateStatusAsync(int enrolmentId, UpdateEnrolmentStatusRequest request)
    {
        if (!EnrolmentStatus.IsValid(request.Status))
        {
            return ServiceResult<EnrolmentDto>.BadRequest(
                $"Status must be one of: {string.Join(", ", EnrolmentStatus.All)}.");
        }

        var enrolment = await _db.Enrolments.Include(en => en.Event).SingleOrDefaultAsync(en => en.EnrolmentId == enrolmentId);
        if (enrolment is null)
        {
            return ServiceResult<EnrolmentDto>.NotFound($"Enrolment {enrolmentId} was not found.");
        }

        if (enrolment.Event.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<EnrolmentDto>.Forbidden("You can only manage enrolments for events that you created.");
        }

        enrolment.Status = EnrolmentStatus.Normalise(request.Status);
        await _db.SaveChangesAsync();

        var dto = await Query().Where(en => en.EnrolmentId == enrolmentId).Select(Projection).SingleAsync();
        return ServiceResult<EnrolmentDto>.Ok(dto);
    }

    private IQueryable<Enrolment> Query() => _db.Enrolments.AsNoTracking();

    private static readonly System.Linq.Expressions.Expression<Func<Enrolment, EnrolmentDto>> Projection = en => new EnrolmentDto
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant.FullName,
        ParticipantEmail = en.Participant.Email,
        EventId = en.EventId,
        EventName = en.Event.Name,
        EventDate = en.Event.EventDate,
        CategoryId = en.CategoryId,
        CategoryName = en.Category.CategoryName,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status,
        HasResult = en.Result != null
    };
}
