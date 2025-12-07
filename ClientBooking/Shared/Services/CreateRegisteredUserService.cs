using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Features.Registration;
using ClientBooking.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace ClientBooking.Shared.Services;

public interface ICreateRegisteredUserService
{
    Task<User> CreateUserWithDefaultSettings(RegistrationRequest request, string hashedPassword);
}

public class CreateRegisteredUserService(DataContext dataContext, PasswordHasher<User> passwordHasher) : ICreateRegisteredUserService
{
    //Create and save the new user with a default assigned role to the database.
    public async Task<User> CreateUserWithDefaultSettings(RegistrationRequest request, string inboundPassword)
    {
        var defaultRole = await dataContext.Roles.FirstOrDefaultAsync(x => x.Name == RoleName.User) ?? await CreateDefaultRole();
        var currentSettings = await dataContext.Settings.OrderByDescending(x => x.Id).FirstAsync();

        var user = request.MapRegistrationRequestToUser(defaultRole, currentSettings);
        var hashedPassword = passwordHasher.HashPassword(user, inboundPassword);
        user.HashedPassword = hashedPassword;
        
        await dataContext.Users.AddAsync(user);
        await dataContext.SaveChangesAsync();

        return user;
    }
    
    //In the rare case our default role doesn't exist, create it.
    private async Task<Role> CreateDefaultRole()
    {
        var defaultRole = new Role{Name = RoleName.User};
        await dataContext.AddAsync(defaultRole);
        
        await dataContext.SaveChangesAsync();
        return defaultRole;
    }
}