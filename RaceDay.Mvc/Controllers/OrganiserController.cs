using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var events = await GetMineAsync();
        return View(events);
    }

    public async Task<IActionResult> Events()
    {
        var events = await GetMineAsync();
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        var vm = new EventFormVm { EventTypes = await LoadEventTypesAsync() };
        return View("EventForm", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        vm.EventTypes = await LoadEventTypesAsync();
        if (!ModelState.IsValid)
        {
            return View("EventForm", vm);
        }

        var response = await _api.SendAsync(HttpMethod.Post, "api/events", vm.ToRequest());
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await _api.ReadErrorAsync(response));
            return View("EventForm", vm);
        }

        var created = await _api.ReadAsync<EventDto>(response);
        if (created is not null && vm.Banner is { Length: > 0 })
        {
            var upload = await _api.UploadFileAsync($"api/events/{created.EventId}/banner", vm.Banner);
            if (!upload.IsSuccessStatusCode)
            {
                TempData["Error"] = "Event saved, but the banner did not upload: " + await _api.ReadErrorAsync(upload);
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        var dto = await _api.ReadAsync<EventDto>(response);
        if (dto is null)
        {
            TempData["Error"] = await _api.ReadErrorAsync(response);
            return RedirectToAction(nameof(Events));
        }

        return View("EventForm", EventFormVm.FromDto(dto, await LoadEventTypesAsync()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormVm vm)
    {
        vm.EventId = id;
        vm.EventTypes = await LoadEventTypesAsync();
        if (!ModelState.IsValid)
        {
            return View("EventForm", vm);
        }

        var response = await _api.SendAsync(HttpMethod.Put, $"api/events/{id}", vm.ToRequest());
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await _api.ReadErrorAsync(response));
            return View("EventForm", vm);
        }

        if (vm.Banner is { Length: > 0 })
        {
            var upload = await _api.UploadFileAsync($"api/events/{id}/banner", vm.Banner);
            if (!upload.IsSuccessStatusCode)
            {
                TempData["Error"] = "Event saved, but the banner did not upload: " + await _api.ReadErrorAsync(upload);
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var response = await _api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Event deleted."
            : await _api.ReadErrorAsync(response);
        return RedirectToAction(nameof(Events));
    }

    public async Task<IActionResult> Categories(int eventId)
    {
        var eventResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}");
        var eventDto = await _api.ReadAsync<EventDto>(eventResponse);
        if (eventDto is null)
        {
            TempData["Error"] = await _api.ReadErrorAsync(eventResponse);
            return RedirectToAction(nameof(Events));
        }

        var catResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}/categories");
        ViewBag.Event = eventDto;
        var categories = await _api.ReadAsync<List<CategoryDto>>(catResponse) ?? [];
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
        }

        var response = await _api.SendAsync(HttpMethod.Post, $"api/events/{vm.EventId}/categories", new CategoryRequest
        {
            CategoryName = vm.CategoryName,
            Description = vm.Description
        });

        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Category added."
            : await _api.ReadErrorAsync(response);
        return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryId, int eventId)
    {
        var response = await _api.SendAsync(HttpMethod.Delete, $"api/categories/{categoryId}");
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Category deleted."
            : await _api.ReadErrorAsync(response);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    public async Task<IActionResult> Enrolments(int? eventId)
    {
        var events = await GetMineAsync();
        ViewBag.Events = events;
        if (events.Count == 0)
        {
            return View(new List<EnrolmentDto>());
        }

        var selectedId = eventId ?? events[0].EventId;
        ViewBag.SelectedEventId = selectedId;
        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{selectedId}/enrolments");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await _api.ReadErrorAsync(response);
            return View(new List<EnrolmentDto>());
        }

        var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? [];
        return View(enrolments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrolmentStatus(int enrolmentId, int eventId, string status)
    {
        var response = await _api.SendAsync(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status",
            new UpdateEnrolmentStatusRequest { Status = status });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Enrolment status updated."
            : await _api.ReadErrorAsync(response);
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    public async Task<IActionResult> Results(int? eventId)
    {
        var events = await GetMineAsync();
        ViewBag.Events = events;
        if (events.Count == 0)
        {
            return View(new List<EnrolmentDto>());
        }

        var selectedId = eventId ?? events[0].EventId;
        ViewBag.SelectedEventId = selectedId;
        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{selectedId}/enrolments");
        var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? [];
        return View(enrolments);
    }

    [HttpGet]
    public async Task<IActionResult> CaptureResult(int enrolmentId, int eventId)
    {
        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}/enrolments");
        var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? [];
        var enrolment = enrolments.FirstOrDefault(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null)
        {
            TempData["Error"] = "That enrolment was not found on this event.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        return View(new CaptureResultVm
        {
            EnrolmentId = enrolment.EnrolmentId,
            EventId = eventId,
            ParticipantName = enrolment.ParticipantName,
            EventName = enrolment.EventName,
            CategoryName = enrolment.CategoryName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureResult(CaptureResultVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var response = await _api.SendAsync(HttpMethod.Post, $"api/enrolments/{vm.EnrolmentId}/result", new CreateResultRequest
        {
            FinishTime = vm.FinishTime,
            FinishingPosition = vm.FinishingPosition,
            Notes = vm.Notes
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await _api.ReadErrorAsync(response));
            return View(vm);
        }

        TempData["Success"] = "Result saved.";
        return RedirectToAction(nameof(Results), new { eventId = vm.EventId });
    }

    private async Task<List<EventDto>> GetMineAsync()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
        return await _api.ReadAsync<List<EventDto>>(response) ?? [];
    }

    private async Task<IEnumerable<SelectListItem>> LoadEventTypesAsync()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "api/eventtypes");
        var types = await _api.ReadAsync<List<EventTypeDto>>(response) ?? [];
        return types.Select(t => new SelectListItem(t.TypeName, t.EventTypeId.ToString()));
    }
}
