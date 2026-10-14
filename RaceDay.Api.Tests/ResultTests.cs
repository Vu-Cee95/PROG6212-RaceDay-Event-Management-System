using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

// Organisers capture results for their events; Participants view their own results.
public class ResultTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ResultTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private static CreateResultRequest SampleResult(int position = 1) => new()
    {
        FinishTime = new TimeSpan(1, 42, 17),
        FinishingPosition = position,
        Notes = "Automated test result"
    };

    // ---------------------------------------------------------------- Capture (Organiser)

    [Fact]
    public async Task CaptureResult_AsOwner_Returns201AndPersists()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult(7));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(enrolment.EnrolmentId, dto!.EnrolmentId);
        Assert.Equal(new TimeSpan(1, 42, 17), dto.FinishTime);
        Assert.Equal(7, dto.FinishingPosition);
        Assert.Equal(1, dto.TotalFinishers);
        Assert.Equal(scenario.Event.Name, dto.EventName);

        var stored = await _factory.QueryDbAsync(db => db.Results.SingleAsync(r => r.EnrolmentId == enrolment.EnrolmentId));
        Assert.Equal(7, stored.FinishingPosition);
        Assert.Equal("Automated test result", stored.Notes);
    }

    [Fact]
    public async Task CaptureResult_ConfirmsAPendingEnrolment()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);
        Assert.Equal(EnrolmentStatus.Pending, enrolment.Status);

        (await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult())).EnsureSuccessStatusCode();

        var stored = await _factory.QueryDbAsync(db => db.Enrolments.SingleAsync(en => en.EnrolmentId == enrolment.EnrolmentId));
        Assert.Equal(EnrolmentStatus.Confirmed, stored.Status);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);
        (await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult())).EnsureSuccessStatusCode();

        var response = await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult(2));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsDifferentOrganiser_Returns403()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        using var other = _factory.CreateSessionClient();
        await other.RegisterAndLoginOrganiserAsync();
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var exists = await _factory.QueryDbAsync(db => db.Results.AnyAsync(r => r.EnrolmentId == enrolment.EnrolmentId));
        Assert.False(exists);
    }

    [Fact]
    public async Task CaptureResult_UnknownEnrolment_Returns404()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.PostAsJsonAsync("/api/enrolments/999999/result", SampleResult());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithZeroPosition_Returns400()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult(0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithZeroFinishTime_Returns400()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            new CreateResultRequest { FinishTime = TimeSpan.Zero, FinishingPosition = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------------------------------------------------------------- My results (Participant)

    [Fact]
    public async Task GetMyResults_ReturnsOnlyOwnResults_WithRaceContext()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);
        (await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", SampleResult(3))).EnsureSuccessStatusCode();

        var mine = await scenario.Participant.GetFromJsonAsync<List<ResultDto>>($"/api/users/{scenario.ParticipantUser.UserId}/results");

        var only = Assert.Single(mine!);
        Assert.Equal(scenario.Event.Name, only.EventName);
        Assert.Equal(scenario.Event.EventDate, only.EventDate);
        Assert.Equal(scenario.Category.CategoryName, only.CategoryName);
        Assert.Equal(new TimeSpan(1, 42, 17), only.FinishTime);
        Assert.Equal(3, only.FinishingPosition);
    }

    [Fact]
    public async Task GetMyResults_SeededParticipant_SeesSeededHistory()
    {
        using var client = _factory.CreateSessionClient();
        var lwando = await client.LoginAsSeedParticipantAsync();

        var mine = await client.GetFromJsonAsync<List<ResultDto>>($"/api/users/{lwando.UserId}/results");

        Assert.Equal(2, mine!.Count);
        Assert.Contains(mine, r => r.EventName == "Soweto Marathon" && r.FinishTime == new TimeSpan(3, 45, 22) && r.FinishingPosition == 120);
        Assert.Contains(mine, r => r.EventName == "Durban Park Run" && r.FinishTime == new TimeSpan(0, 28, 45) && r.FinishingPosition == 45);
    }

    [Fact]
    public async Task GetResults_OfAnotherUser_Returns403()
    {
        using var client = _factory.CreateSessionClient();
        var me = await client.RegisterAndLoginParticipantAsync();

        var response = await client.GetAsync($"/api/users/{me.UserId + 1000}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsOrganiser_Returns403()
    {
        using var client = _factory.CreateSessionClient();
        var organiser = await client.RegisterAndLoginOrganiserAsync();

        var response = await client.GetAsync($"/api/users/{organiser.UserId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------- Event results (Organiser)

    [Fact]
    public async Task GetEventResults_AsOwner_ReturnsResultsOrderedByPosition()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        using var second = _factory.CreateSessionClient();
        await second.RegisterAndLoginParticipantAsync();
        var first = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);
        var other = await second.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);
        (await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{first.EnrolmentId}/result", SampleResult(2))).EnsureSuccessStatusCode();
        (await scenario.Organiser.PostAsJsonAsync($"/api/enrolments/{other.EnrolmentId}/result", SampleResult(1))).EnsureSuccessStatusCode();

        var results = await scenario.Organiser.GetFromJsonAsync<List<ResultDto>>($"/api/events/{scenario.Event.EventId}/results");

        Assert.Equal(2, results!.Count);
        Assert.Equal(1, results[0].FinishingPosition);
        Assert.Equal(2, results[1].FinishingPosition);
        Assert.All(results, r => Assert.Equal(2, r.TotalFinishers));
    }

    [Fact]
    public async Task GetEventResults_AsDifferentOrganiser_Returns403()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        using var other = _factory.CreateSessionClient();
        await other.RegisterAndLoginOrganiserAsync();

        var response = await other.GetAsync($"/api/events/{scenario.Event.EventId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
