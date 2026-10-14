using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

// Participants enrol in events; Organisers view and manage enrolments for their events.
public class EnrolmentTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EnrolmentTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------- Enrol (Participant)

    [Fact]
    public async Task Enrol_AsParticipant_Returns201AndRecordsParticipantEventAndCategory()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);

        var response = await scenario.Participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = scenario.Event.EventId, CategoryId = scenario.Category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.True(dto!.EnrolmentId > 0);
        Assert.Equal(scenario.ParticipantUser.UserId, dto.ParticipantId);
        Assert.Equal(scenario.Event.EventId, dto.EventId);
        Assert.Equal(scenario.Category.CategoryId, dto.CategoryId);
        Assert.Equal(EnrolmentStatus.Pending, dto.Status);
        Assert.False(dto.HasResult);

        // The link is correctly recorded in the database.
        var stored = await _factory.QueryDbAsync(db => db.Enrolments.SingleAsync(en => en.EnrolmentId == dto.EnrolmentId));
        Assert.Equal(scenario.ParticipantUser.UserId, stored.ParticipantId);
        Assert.Equal(scenario.Event.EventId, stored.EventId);
        Assert.Equal(scenario.Category.CategoryId, stored.CategoryId);
        Assert.Equal(EnrolmentStatus.Pending, stored.Status);
    }

    [Fact]
    public async Task Enrol_TwiceInSameEvent_Returns409()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = scenario.Event.EventId, CategoryId = scenario.Category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var count = await _factory.QueryDbAsync(db => db.Enrolments.CountAsync(
            en => en.ParticipantId == scenario.ParticipantUser.UserId && en.EventId == scenario.Event.EventId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_Returns400()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var otherEvent = await scenario.Organiser.CreateEventAsync();
        var otherCategory = await scenario.Organiser.CreateCategoryAsync(otherEvent.EventId, "Elsewhere");

        var response = await scenario.Participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = scenario.Event.EventId, CategoryId = otherCategory.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("does not belong", (await response.ReadErrorAsync()).Message);
    }

    [Fact]
    public async Task Enrol_InUnknownEvent_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginParticipantAsync();

        var response = await client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest { EventId = 999999, CategoryId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithInvalidBody_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginParticipantAsync();

        var response = await client.PostAsJsonAsync("/api/enrolments", new { eventId = 0, categoryId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);

        var response = await scenario.Organiser.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = scenario.Event.EventId, CategoryId = scenario.Category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_Returns401()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest { EventId = 1, CategoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------- My enrolments (Participant)

    [Fact]
    public async Task GetMyEnrolments_ReturnsOnlyTheParticipantsOwnEnrolments()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var mine = await scenario.Participant.GetFromJsonAsync<List<EnrolmentDto>>(
            $"/api/users/{scenario.ParticipantUser.UserId}/enrolments");

        var only = Assert.Single(mine!);
        Assert.Equal(enrolment.EnrolmentId, only.EnrolmentId);
        Assert.Equal(scenario.Event.Name, only.EventName);
        Assert.Equal(scenario.Category.CategoryName, only.CategoryName);
    }

    [Fact]
    public async Task GetMyEnrolments_SeededParticipant_SeesSeededEnrolments()
    {
        using var client = _factory.CreateSessionClient();
        var lwando = await client.LoginAsSeedParticipantAsync();

        var mine = await client.GetFromJsonAsync<List<EnrolmentDto>>($"/api/users/{lwando.UserId}/enrolments");

        Assert.Equal(2, mine!.Count);
        Assert.Contains(mine, en => en.EventName == "Soweto Marathon" && en.CategoryName == "Senior" && en.Status == EnrolmentStatus.Confirmed && en.HasResult);
        Assert.Contains(mine, en => en.EventName == "Durban Park Run" && en.CategoryName == "Walkers");
    }

    [Fact]
    public async Task GetEnrolments_OfAnotherUser_Returns403()
    {
        using var client = _factory.CreateSessionClient();
        var me = await client.RegisterAndLoginParticipantAsync();

        var response = await client.GetAsync($"/api/users/{me.UserId + 1000}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------- Event enrolments (Organiser)

    [Fact]
    public async Task GetEventEnrolments_AsOwner_ListsAllParticipants()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        using var second = _factory.CreateSessionClient();
        var secondUser = await second.RegisterAndLoginParticipantAsync();
        await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);
        await second.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var list = await scenario.Organiser.GetFromJsonAsync<List<EnrolmentDto>>($"/api/events/{scenario.Event.EventId}/enrolments");

        Assert.Equal(2, list!.Count);
        Assert.Contains(list, en => en.ParticipantId == scenario.ParticipantUser.UserId);
        Assert.Contains(list, en => en.ParticipantId == secondUser.UserId);
        Assert.All(list, en => Assert.Equal(scenario.Event.EventId, en.EventId));
    }

    [Fact]
    public async Task GetEventEnrolments_AsDifferentOrganiser_Returns403()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        using var other = _factory.CreateSessionClient();
        await other.RegisterAndLoginOrganiserAsync();

        var response = await other.GetAsync($"/api/events/{scenario.Event.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrolments_AsParticipant_Returns403()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);

        var response = await scenario.Participant.GetAsync($"/api/events/{scenario.Event.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrolments_UnknownEvent_Returns404()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.GetAsync("/api/events/999999/enrolments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------------------------------------------------------- Status (Organiser)

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Cancelled")]
    [InlineData("pending")]
    public async Task UpdateStatus_AsOwner_Returns200AndPersists(string status)
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { Status = status });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await _factory.QueryDbAsync(db => db.Enrolments.SingleAsync(en => en.EnrolmentId == enrolment.EnrolmentId));
        Assert.Equal(EnrolmentStatus.Normalise(status), stored.Status);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidValue_Returns400()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { Status = "Maybe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_AsDifferentOrganiser_Returns403()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        using var other = _factory.CreateSessionClient();
        await other.RegisterAndLoginOrganiserAsync();
        var enrolment = await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await other.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_UnknownEnrolment_Returns404()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.PutAsJsonAsync("/api/enrolments/999999/status", new UpdateEnrolmentStatusRequest { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
