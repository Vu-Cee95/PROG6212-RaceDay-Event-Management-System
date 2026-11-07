using RaceDay.Contracts;

namespace RaceDay.Mvc.Helpers;

public static class EventImages
{
    public static string Url(EventDto eventDto)
    {
        if (!string.IsNullOrWhiteSpace(eventDto.BannerImageUrl))
        {
            return eventDto.BannerImageUrl;
        }

        return Fallback(eventDto.EventType);
    }

    public static string Fallback(string? eventType) =>
        (eventType ?? "Run").ToLowerInvariant() switch
        {
            "walk" => "/img/walk.jpg",
            "cycle" => "/img/cycle.jpg",
            _ => "/img/run.jpg"
        };
}
