using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

// Organisers manage events; everyone can view them.
public class EventManagementTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EventManagementTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------- Viewing

    [Fact]
    public async Task GetAllEvents_WithoutLogin_ReturnsSeededEvents()
    {
        using var client = _factory.CreateSessionClient();

        var events = await client.GetFromJsonAsync<List<EventDto>>("/api/events");

        Assert.NotNull(events);
        Assert.Contains(events!, e => e.Name == "Soweto Marathon" && e.EventType == "Run" && e.Distance == 42.20m);
        Assert.Contains(events!, e => e.Name == "Cape Town Cycle Tour" && e.EventType == "Cycle");
        Assert.Contains(events!, e => e.Name == "Durban Park Run" && e.EventType == "Walk");
    }

    [Fact]
    public async Task GetEventById_ReturnsFullDetails()
    {
        using var client = _factory.CreateSessionClient();
        var all = await client.GetFromJsonAsync<List<EventDto>>("/api/events");
        var soweto = all!.Single(e => e.Name == "Soweto Marathon");

        var ev = await client.GetFromJsonAsync<EventDto>($"/api/events/{soweto.EventId}");

        Assert.Equal("Soweto Marathon", ev!.Name);
        Assert.Equal("Thabo Mokoena", ev.OrganiserName);
        Assert.Equal("Soweto, Johannesburg", ev.Location);
        Assert.Equal(new DateTime(2026, 11, 1, 6, 0, 0), ev.EventDate);
        Assert.Equal(2, ev.CategoryCount);
        Assert.Equal(2, ev.EnrolmentCount);
    }

    [Fact]
    public async Task GetEventById_UnknownId_Returns404()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEventTypes_ReturnsRunWalkCycle()
    {
        using var client = _factory.CreateSessionClient();

        var types = await client.GetFromJsonAsync<List<EventTypeDto>>("/api/eventtypes");

        Assert.Equal(new[] { "Run", "Walk", "Cycle" }, types!.Select(t => t.TypeName).ToArray());
    }

    // ---------------------------------------------------------------- Create

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201AndPersistsAllFields()
    {
        using var client = _factory.CreateSessionClient();
        var organiser = await client.RegisterAndLoginOrganiserAsync();
        var request = new CreateEventRequest
        {
            Name = "Two Oceans Half",
            Description = "Scenic half marathon around the Cape Peninsula.",
            EventDate = new DateTime(2027, 4, 3, 6, 30, 0),
            Location = "Cape Town, Western Cape",
            Distance = 21.10m,
            EventTypeId = 1
        };

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.True(created!.EventId > 0);
        Assert.Equal(organiser.UserId, created.OrganiserId);
        Assert.Equal("Run", created.EventType);

        var stored = await _factory.QueryDbAsync(db => db.Events.SingleAsync(e => e.EventId == created.EventId));
        Assert.Equal("Two Oceans Half", stored.Name);
        Assert.Equal("Scenic half marathon around the Cape Peninsula.", stored.Description);
        Assert.Equal(new DateTime(2027, 4, 3, 6, 30, 0), stored.EventDate);
        Assert.Equal("Cape Town, Western Cape", stored.Location);
        Assert.Equal(21.10m, stored.Distance);
        Assert.Equal(1, stored.EventTypeId);
        Assert.Equal(organiser.UserId, stored.OrganiserId);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403AndNothingIsSaved()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginParticipantAsync();
        var request = ApiClientExtensions.NewEventRequest("Participant Should Not Create This");

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var exists = await _factory.QueryDbAsync(db => db.Events.AnyAsync(e => e.Name == request.Name));
        Assert.False(exists);
    }

    [Fact]
    public async Task CreateEvent_WithUnknownEventType_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var request = ApiClientExtensions.NewEventRequest();
        request.EventTypeId = 99;

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("EventTypeId 99", (await response.ReadErrorAsync()).Message);
    }

    [Fact]
    public async Task CreateEvent_WithMissingRequiredFields_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.PostAsJsonAsync("/api/events", new { description = "no name, date, location, distance or type" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.Contains("Name", error.Errors!.Keys);
        Assert.Contains("Location", error.Errors.Keys);
        Assert.Contains("Distance", error.Errors.Keys);
        Assert.Contains("EventTypeId", error.Errors.Keys);
    }

    // ---------------------------------------------------------------- Update

    [Fact]
    public async Task UpdateEvent_AsOwner_Returns200AndPersists()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var ev = await client.CreateEventAsync();

        var response = await client.PutAsJsonAsync($"/api/events/{ev.EventId}", new UpdateEventRequest
        {
            Name = "Renamed Event",
            Description = "Now a walk.",
            EventDate = new DateTime(2027, 6, 6, 7, 0, 0),
            Location = "Bloemfontein, Free State",
            Distance = 5.00m,
            EventTypeId = 2
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Renamed Event", updated!.Name);
        Assert.Equal("Walk", updated.EventType);
        Assert.Equal(5.00m, updated.Distance);

        var stored = await _factory.QueryDbAsync(db => db.Events.SingleAsync(e => e.EventId == ev.EventId));
        Assert.Equal("Bloemfontein, Free State", stored.Location);
    }

    [Fact]
    public async Task UpdateEvent_AsDifferentOrganiser_Returns403()
    {
        using var owner = _factory.CreateSessionClient();
        using var other = _factory.CreateSessionClient();
        await owner.RegisterAndLoginOrganiserAsync();
        await other.RegisterAndLoginOrganiserAsync();
        var ev = await owner.CreateEventAsync();

        var response = await other.PutAsJsonAsync($"/api/events/{ev.EventId}", ApiClientExtensions.NewEventRequest("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var stored = await _factory.QueryDbAsync(db => db.Events.SingleAsync(e => e.EventId == ev.EventId));
        Assert.Equal(ev.Name, stored.Name);
    }

    [Fact]
    public async Task UpdateEvent_AsParticipant_Returns403()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginParticipantAsync();

        var response = await client.PutAsJsonAsync("/api/events/1", ApiClientExtensions.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_UnknownId_Returns404()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.PutAsJsonAsync("/api/events/999999", ApiClientExtensions.NewEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------------------------------------------------------- Delete

    [Fact]
    public async Task DeleteEvent_AsOwner_Returns204AndRemovesEventAndCategories()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var ev = await client.CreateEventAsync();
        var cat = await client.CreateCategoryAsync(ev.EventId);

        var response = await client.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/events/{ev.EventId}")).StatusCode);
        var categoryStillExists = await _factory.QueryDbAsync(db => db.Categories.AnyAsync(c => c.CategoryId == cat.CategoryId));
        Assert.False(categoryStillExists);
    }

    [Fact]
    public async Task DeleteEvent_AsDifferentOrganiser_Returns403()
    {
        using var owner = _factory.CreateSessionClient();
        using var other = _factory.CreateSessionClient();
        await owner.RegisterAndLoginOrganiserAsync();
        await other.RegisterAndLoginOrganiserAsync();
        var ev = await owner.CreateEventAsync();

        var response = await other.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithExistingEnrolments_Returns409()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Organiser.DeleteAsync($"/api/events/{scenario.Event.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_UnknownId_Returns404()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.DeleteAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------------------------------------------------------- Mine

    [Fact]
    public async Task GetMyEvents_ReturnsOnlyTheOrganisersOwnEvents()
    {
        using var client = _factory.CreateSessionClient();
        var organiser = await client.RegisterAndLoginOrganiserAsync();
        var first = await client.CreateEventAsync();
        var second = await client.CreateEventAsync();

        var mine = await client.GetFromJsonAsync<List<EventDto>>("/api/events/mine");

        Assert.Equal(2, mine!.Count);
        Assert.All(mine, e => Assert.Equal(organiser.UserId, e.OrganiserId));
        Assert.Contains(mine, e => e.EventId == first.EventId);
        Assert.Contains(mine, e => e.EventId == second.EventId);
    }
}
