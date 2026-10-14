using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

// Organisers define categories for their own events; both roles can view them.
public class CategoryTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public CategoryTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCategories_WithoutLogin_ReturnsSeededCategoriesForEvent()
    {
        using var client = _factory.CreateSessionClient();
        var all = await client.GetFromJsonAsync<List<EventDto>>("/api/events");
        var soweto = all!.Single(e => e.Name == "Soweto Marathon");

        var categories = await client.GetFromJsonAsync<List<CategoryDto>>($"/api/events/{soweto.EventId}/categories");

        Assert.Equal(2, categories!.Count);
        Assert.Contains(categories, c => c.CategoryName == "Under 20");
        Assert.Contains(categories, c => c.CategoryName == "Senior");
        Assert.All(categories, c => Assert.Equal(soweto.EventId, c.EventId));
    }

    [Fact]
    public async Task GetCategories_UnknownEvent_Returns404()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsOwner_Returns201AndPersists()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var ev = await client.CreateEventAsync();

        var response = await client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CategoryRequest { CategoryName = "10km", Description = "Ten kilometre route" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("10km", created!.CategoryName);
        Assert.Equal(ev.EventId, created.EventId);

        var stored = await _factory.QueryDbAsync(db => db.Categories.SingleAsync(c => c.CategoryId == created.CategoryId));
        Assert.Equal("Ten kilometre route", stored.Description);
    }

    [Fact]
    public async Task CreateCategory_AsDifferentOrganiser_Returns403()
    {
        using var owner = _factory.CreateSessionClient();
        using var other = _factory.CreateSessionClient();
        await owner.RegisterAndLoginOrganiserAsync();
        await other.RegisterAndLoginOrganiserAsync();
        var ev = await owner.CreateEventAsync();

        var response = await other.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CategoryRequest { CategoryName = "Intruder" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_Returns403()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginParticipantAsync();

        var response = await client.PostAsJsonAsync("/api/events/1/categories", new CategoryRequest { CategoryName = "Nope" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ForUnknownEvent_Returns404()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.PostAsJsonAsync("/api/events/999999/categories", new CategoryRequest { CategoryName = "Ghost" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateNameForSameEvent_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var ev = await client.CreateEventAsync();
        await client.CreateCategoryAsync(ev.EventId, "Senior");

        var response = await client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new CategoryRequest { CategoryName = "Senior" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithEmptyName_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var ev = await client.CreateEventAsync();

        var response = await client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new CategoryRequest { CategoryName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_AsOwner_Returns200()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var ev = await client.CreateEventAsync();
        var cat = await client.CreateCategoryAsync(ev.EventId, "Under 18");

        var response = await client.PutAsJsonAsync($"/api/categories/{cat.CategoryId}",
            new CategoryRequest { CategoryName = "Under 20", Description = "Juniors" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Under 20", updated!.CategoryName);
        Assert.Equal("Juniors", updated.Description);
    }

    [Fact]
    public async Task UpdateCategory_AsDifferentOrganiser_Returns403()
    {
        using var owner = _factory.CreateSessionClient();
        using var other = _factory.CreateSessionClient();
        await owner.RegisterAndLoginOrganiserAsync();
        await other.RegisterAndLoginOrganiserAsync();
        var ev = await owner.CreateEventAsync();
        var cat = await owner.CreateCategoryAsync(ev.EventId);

        var response = await other.PutAsJsonAsync($"/api/categories/{cat.CategoryId}", new CategoryRequest { CategoryName = "Changed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_UnknownId_Returns404()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();

        var response = await client.PutAsJsonAsync("/api/categories/999999", new CategoryRequest { CategoryName = "Ghost" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsOwner_Returns204AndRemovesIt()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginOrganiserAsync();
        var ev = await client.CreateEventAsync();
        var cat = await client.CreateCategoryAsync(ev.EventId);

        var response = await client.DeleteAsync($"/api/categories/{cat.CategoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = await client.GetFromJsonAsync<List<CategoryDto>>($"/api/events/{ev.EventId}/categories");
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task DeleteCategory_AsDifferentOrganiser_Returns403()
    {
        using var owner = _factory.CreateSessionClient();
        using var other = _factory.CreateSessionClient();
        await owner.RegisterAndLoginOrganiserAsync();
        await other.RegisterAndLoginOrganiserAsync();
        var ev = await owner.CreateEventAsync();
        var cat = await owner.CreateCategoryAsync(ev.EventId);

        var response = await other.DeleteAsync($"/api/categories/{cat.CategoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        using var scenario = await RaceEventFixture.CreateAsync(_factory);
        await scenario.Participant.EnrolAsync(scenario.Event.EventId, scenario.Category.CategoryId);

        var response = await scenario.Organiser.DeleteAsync($"/api/categories/{scenario.Category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
