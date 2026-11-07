using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize(Role = "Participant")]
public class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? type, string? search)
    {
        var response = await _api.SendAsync(HttpMethod.Get, "api/events");
        var events = await _api.ReadAsync<List<EventDto>>(response) ?? [];

        if (!string.IsNullOrWhiteSpace(type))
        {
            events = events.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Type = type;
        ViewBag.Search = search;
        ViewBag.Types = new[] { "Run", "Walk", "Cycle" };
        return View(events.OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var eventResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        var eventDto = await _api.ReadAsync<EventDto>(eventResponse);
        if (eventDto is null)
        {
            TempData["Error"] = await _api.ReadErrorAsync(eventResponse);
            return RedirectToAction(nameof(Index));
        }

        var catResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}/categories");
        ViewBag.Categories = await _api.ReadAsync<List<CategoryDto>>(catResponse) ?? [];
        return View(eventDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterEvent(int eventId, int categoryId)
    {
        var response = await _api.SendAsync(HttpMethod.Post, "api/enrolments", new CreateEnrolmentRequest
        {
            EventId = eventId,
            CategoryId = categoryId
        });

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await _api.ReadErrorAsync(response);
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You have entered the event.";
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyEvents()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        var response = await _api.SendAsync(HttpMethod.Get, $"api/users/{userId}/enrolments");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await _api.ReadErrorAsync(response);
            return View(new List<EnrolmentDto>());
        }

        return View(await _api.ReadAsync<List<EnrolmentDto>>(response) ?? []);
    }

    public async Task<IActionResult> MyResults()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        var response = await _api.SendAsync(HttpMethod.Get, $"api/users/{userId}/results");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await _api.ReadErrorAsync(response);
            return View(new List<ResultDto>());
        }

        return View(await _api.ReadAsync<List<ResultDto>>(response) ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "api/users/profile");
        var dto = await _api.ReadAsync<ProfileDto>(response);
        if (dto is null)
        {
            TempData["Error"] = await _api.ReadErrorAsync(response);
            return RedirectToAction(nameof(Index));
        }

        return View(new ProfileVm
        {
            Email = dto.Email,
            Role = dto.Role,
            FullName = dto.FullName,
            ProfilePictureUrl = dto.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var update = await _api.SendAsync(HttpMethod.Put, "api/users/profile", new UpdateProfileRequest
        {
            FullName = vm.FullName,
            ProfilePictureUrl = vm.ProfilePictureUrl
        });

        if (!update.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await _api.ReadErrorAsync(update));
            return View(vm);
        }

        if (vm.Picture is { Length: > 0 })
        {
            var upload = await _api.UploadFileAsync("api/users/profile/picture", vm.Picture);
            if (!upload.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await _api.ReadErrorAsync(upload));
                return View(vm);
            }

            var updated = await _api.ReadAsync<ProfileDto>(upload);
            if (updated is not null)
            {
                vm.ProfilePictureUrl = updated.ProfilePictureUrl;
            }
        }

        HttpContext.Session.SetString(SessionKeys.DisplayName, vm.FullName);
        TempData["Success"] = "Profile saved.";
        return RedirectToAction(nameof(Profile));
    }
}
