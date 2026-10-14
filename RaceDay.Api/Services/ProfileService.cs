using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Services;

public interface IProfileService
{
    Task<ServiceResult<ProfileDto>> GetMyProfileAsync();
    Task<ServiceResult<ProfileDto>> UpdateMyProfileAsync(UpdateProfileRequest request);
}

// Profile operations always act on the user identified by the session - a user can only ever read or update their own account.
public class ProfileService : IProfileService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProfileService(RaceDayDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<ProfileDto>> GetMyProfileAsync()
    {
        var user = await _db.Users.FindAsync(_currentUser.UserId);
        return user is null
            ? ServiceResult<ProfileDto>.NotFound("Your account could not be found.")
            : ServiceResult<ProfileDto>.Ok(ToDto(user));
    }

    public async Task<ServiceResult<ProfileDto>> UpdateMyProfileAsync(UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(_currentUser.UserId);
        if (user is null)
        {
            return ServiceResult<ProfileDto>.NotFound("Your account could not be found.");
        }

        user.FullName = request.FullName.Trim();
        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(request.ProfilePictureUrl)
            ? null
            : request.ProfilePictureUrl.Trim();

        await _db.SaveChangesAsync();
        return ServiceResult<ProfileDto>.Ok(ToDto(user));
    }

    public static ProfileDto ToDto(User user) => new()
    {
        UserId = user.UserId,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role,
        ProfilePictureUrl = user.ProfilePictureUrl,
        DateRegistered = user.DateRegistered
    };
}
