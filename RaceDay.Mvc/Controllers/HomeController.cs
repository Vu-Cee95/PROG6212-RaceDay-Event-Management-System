using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;

    public HomeController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString(SessionKeys.Role);
        if (string.Equals(role, "Organiser", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Organiser");
        }

        if (string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Participant");
        }

        var response = await _api.SendAsync(HttpMethod.Get, "api/events");
        var events = await _api.ReadAsync<List<EventDto>>(response) ?? [];
        return View(events.OrderBy(e => e.EventDate).ToList());
    }

    public IActionResult AccessDenied() => View();

    public IActionResult Error() => View("~/Views/Shared/Error.cshtml");
}
