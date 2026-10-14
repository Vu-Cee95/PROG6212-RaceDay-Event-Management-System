using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// View and update the logged-in user's own profile (both roles).
[ApiController]
[Route("api/users/profile")]
[Tags("User Profile")]
[Produces("application/json")]
[RequireAuthenticated]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profiles;

    public ProfileController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    // Get my profile.
    [HttpGet]
    [SwaggerOperation(Summary = "Get the logged-in user's profile",
        Description = "Any logged-in user (Organiser or Participant). The user is resolved from the session, never from the URL.")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _profiles.GetMyProfileAsync();
        return result.ToActionResult(this);
    }

    // Update my profile.
    [HttpPut]
    [SwaggerOperation(Summary = "Update the logged-in user's profile",
        Description = "Any logged-in user. Body: { fullName, profilePictureUrl }. Email and role cannot be changed here.")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var result = await _profiles.UpdateMyProfileAsync(request);
        return result.ToActionResult(this);
    }
}
