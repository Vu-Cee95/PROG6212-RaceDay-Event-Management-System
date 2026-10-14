using System.Net;
using System.Net.Http.Json;
using System.Text;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

// Cross-cutting HTTP behaviour that every endpoint shares: the health probe, unknown routes, malformed JSON and the ApiError shape for validation failures.
public class ApiBehaviourTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ApiBehaviourTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJson_Returns400WithApiError()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{ this is not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.Equal("One or more validation errors occurred.", error.Message);
    }

    [Fact]
    public async Task ValidationFailure_ListsEachInvalidField()
    {
        var client = _factory.CreateClient();

        // Missing email, short password, missing name and invalid role - all at once.
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "",
            password = "short",
            fullName = "",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.NotNull(error.Errors);
        Assert.Contains("Email", error.Errors!.Keys);
        Assert.Contains("Password", error.Errors.Keys);
        Assert.Contains("FullName", error.Errors.Keys);
    }

    [Fact]
    public async Task SwaggerDocument_IsServedAndListsEveryTag()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        foreach (var tag in new[] { "Auth", "User Profile", "Event Types", "Events", "Categories", "Enrolments", "Results" })
        {
            Assert.Contains($"\"{tag}\"", json);
        }
    }
}
