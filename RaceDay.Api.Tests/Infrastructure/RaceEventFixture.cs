using RaceDay.Contracts;

namespace RaceDay.Api.Tests.Infrastructure;

// A self-contained scenario: a fresh Organiser who owns a fresh Event with one Category, plus a fresh Participant.
public sealed class RaceEventFixture : IDisposable
{
    public HttpClient Organiser { get; }
    public HttpClient Participant { get; }
    public AuthResponse OrganiserUser { get; private set; } = null!;
    public AuthResponse ParticipantUser { get; private set; } = null!;
    public EventDto Event { get; private set; } = null!;
    public CategoryDto Category { get; private set; } = null!;

    private RaceEventFixture(RaceDayApiFactory factory)
    {
        Organiser = factory.CreateSessionClient();
        Participant = factory.CreateSessionClient();
    }

    public static async Task<RaceEventFixture> CreateAsync(RaceDayApiFactory factory)
    {
        var fixture = new RaceEventFixture(factory);
        fixture.OrganiserUser = await fixture.Organiser.RegisterAndLoginOrganiserAsync();
        fixture.ParticipantUser = await fixture.Participant.RegisterAndLoginParticipantAsync();
        fixture.Event = await fixture.Organiser.CreateEventAsync();
        fixture.Category = await fixture.Organiser.CreateCategoryAsync(fixture.Event.EventId);
        return fixture;
    }

    public void Dispose()
    {
        Organiser.Dispose();
        Participant.Dispose();
    }
}
