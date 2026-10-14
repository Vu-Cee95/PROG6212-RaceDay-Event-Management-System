using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Services;

public interface IResultService
{
    Task<ServiceResult<ResultDto>> CaptureAsync(int enrolmentId, CreateResultRequest request);
    Task<ServiceResult<IReadOnlyList<ResultDto>>> GetForParticipantAsync(int userId);
    Task<ServiceResult<IReadOnlyList<ResultDto>>> GetForEventAsync(int eventId);
}

/// <summary>
/// Race results. The Organiser who owns an Event captures a finish time and
/// position against each Enrolment (one Result per Enrolment). Participants view
/// their own results; Organisers view results for their events.
/// </summary>
public class ResultService : IResultService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ResultService(RaceDayDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<ResultDto>> CaptureAsync(int enrolmentId, CreateResultRequest request)
    {
        if (request.FinishTime <= TimeSpan.Zero || request.FinishTime >= TimeSpan.FromDays(1))
        {
            return ServiceResult<ResultDto>.BadRequest("FinishTime must be between 00:00:01 and 23:59:59.");
        }

        var enrolment = await _db.Enrolments.Include(en => en.Event).SingleOrDefaultAsync(en => en.EnrolmentId == enrolmentId);
        if (enrolment is null)
        {
            return ServiceResult<ResultDto>.NotFound($"Enrolment {enrolmentId} was not found.");
        }

        if (enrolment.Event.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<ResultDto>.Forbidden("You can only capture results for events that you created.");
        }

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
        {
            return ServiceResult<ResultDto>.Conflict("A result has already been captured for this enrolment.");
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            FinishingPosition = request.FinishingPosition,
            Notes = request.Notes?.Trim()
        };

        _db.Results.Add(result);

        // A participant with a recorded result has clearly taken part - confirm the enrolment.
        if (enrolment.Status == EnrolmentStatus.Pending)
        {
            enrolment.Status = EnrolmentStatus.Confirmed;
        }

        await _db.SaveChangesAsync();

        var dto = await Query().Where(r => r.ResultId == result.ResultId).Select(Projection).SingleAsync();
        return ServiceResult<ResultDto>.Created(dto);
    }

    public async Task<ServiceResult<IReadOnlyList<ResultDto>>> GetForParticipantAsync(int userId)
    {
        if (userId != _currentUser.UserId)
        {
            return ServiceResult<IReadOnlyList<ResultDto>>.Forbidden("You can only view your own results.");
        }

        var list = await Query()
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .Select(Projection)
            .ToListAsync();

        return ServiceResult<IReadOnlyList<ResultDto>>.Ok(list);
    }

    public async Task<ServiceResult<IReadOnlyList<ResultDto>>> GetForEventAsync(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null)
        {
            return ServiceResult<IReadOnlyList<ResultDto>>.NotFound($"Event {eventId} was not found.");
        }

        if (ev.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<IReadOnlyList<ResultDto>>.Forbidden("You can only view results for events that you created.");
        }

        var list = await Query()
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.FinishingPosition)
            .Select(Projection)
            .ToListAsync();

        return ServiceResult<IReadOnlyList<ResultDto>>.Ok(list);
    }

    private IQueryable<Result> Query() => _db.Results.AsNoTracking();

    private static readonly System.Linq.Expressions.Expression<Func<Result, ResultDto>> Projection = r => new ResultDto
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FullName,
        EventId = r.Enrolment.EventId,
        EventName = r.Enrolment.Event.Name,
        EventDate = r.Enrolment.Event.EventDate,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime,
        FinishingPosition = r.FinishingPosition,
        TotalFinishers = r.Enrolment.Event.Enrolments.Count(en => en.Result != null),
        Notes = r.Notes
    };
}
