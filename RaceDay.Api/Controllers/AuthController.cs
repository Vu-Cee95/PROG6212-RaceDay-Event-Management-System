using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Registration, login and logout for Organisers and Participants.
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // Register a new account.
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register a new Organiser or Participant",
        Description = "Public. Validates the details, checks the email is not already in use, hashes the password and saves the user.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        return result.ToActionResult(this);
    }

    // Log in with email and password.
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Log in and start a session",
        Description = "Public. On success a session cookie is issued that stores the UserId and Role for all later requests.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return result.ToActionResult(this);
    }

    // Log out and end the session.
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Log out and clear the session",
        Description = "Any caller. Clears the server-side session; safe to call even when not logged in.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        _auth.Logout();
        return NoContent();
    }
}
