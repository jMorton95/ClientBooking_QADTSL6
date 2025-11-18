using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Features.Registration;

public class RegistrationRequest
{
    public string FirstName { get; set; } = "";
    
    public string LastName { get; set; } = "";
    
    public string Email { get; set; } = "";
    
    public string PasswordOne { get; set; } = "";
    
    public string PasswordTwo { get; set; } = "";

    //Convert a password from a RegistrationRequest into a readily save-able database object.
    public User MapRegistrationRequestToUser(string hashedPassword, Role defaultRole, Settings settings) => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        IsActive = true,
        HashedPassword = hashedPassword,
        IsLockedOut = false,
        LockoutEnd = null,
        AccessFailedCount = 0,
        WorkingHoursStart = settings.DefaultWorkingHoursStart,
        WorkingHoursEnd = settings.DefaultWorkingHoursEnd,
        BreakTimeStart =  settings.DefaultBreakTimeStart,
        BreakTimeEnd = settings.DefaultBreakTimeEnd,
        UserRoles = [new UserRole{RoleId = defaultRole.Id}]
    };
};