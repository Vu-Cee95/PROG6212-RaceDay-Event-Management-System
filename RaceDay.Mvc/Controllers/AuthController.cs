using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class AuthController : Controller
{
    private readonly RaceDayApiClient _api;

    public AuthController(RaceDayApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/register", new RegisterRequest
        {
            FullName = vm.FullName,
            Email = vm.Email,
            Password = vm.Password,
            Role = vm.Role
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await _api.ReadErrorAsync(response));
            return View(vm);
        }

        TempData["Success"] = "Registration successful. Log in with your new account.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/login", new LoginRequest
        {
            Email = vm.Email,
            Password = vm.Password
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(vm);
        }

        var auth = await _api.ReadAsync<AuthResponse>(response);
        if (auth is null)
        {
            ModelState.AddModelError(string.Empty, "Login response could not be read.");
            return View(vm);
        }

        var apiCookie = RaceDayApiClient.ExtractSessionCookie(response);
        if (!string.IsNullOrWhiteSpace(apiCookie))
        {
            HttpContext.Session.SetString(SessionKeys.ApiSessionCookie, apiCookie);
        }

        HttpContext.Session.SetInt32(SessionKeys.UserId, auth.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, auth.Role);
        HttpContext.Session.SetString(SessionKeys.DisplayName, string.IsNullOrWhiteSpace(auth.FullName) ? auth.Email : auth.FullName);

        if (auth.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Organiser");
        }

        return RedirectToAction("Index", "Participant");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.SendAsync(HttpMethod.Post, "api/auth/logout");
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }
}
