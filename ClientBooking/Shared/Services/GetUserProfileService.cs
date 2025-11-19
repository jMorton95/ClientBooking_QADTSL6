using ClientBooking.Data;
using ClientBooking.Shared.Mapping;
using ClientBooking.Shared.Models;

namespace ClientBooking.Shared.Services;

public interface IGetUserProfileService
{
    Task<UserProfile?> GetUserSessionProfile(int userId);
}

public class GetUserProfileService(DataContext dataContext) : IGetUserProfileService
{
    public async Task<UserProfile?> GetUserSessionProfile(int userId)
    {
        var user = await dataContext.Users.FindAsync(userId);

        return user?.MapToUserProfile();
    }
}