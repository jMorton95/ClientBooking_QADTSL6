using ClientBooking.Data.Entities;

namespace ClientBooking.Features.Registration;

public class RegistrationRequest
{
    public string FirstName { get; set; } = "";
    
    public string LastName { get; set; } = "";
    
    public string Email { get; set; } = "";
    
    public string PasswordOne { get; set; } = "";
    
    public string PasswordTwo { get; set; } = "";

    public User MapRegistrationRequestToUser(string hashedPassword) => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        IsActive = true,
        HashedPassword = hashedPassword,
        IsLockedOut = false,
        LockoutEnd = null,
        AccessFailedCount = 0,
    };
};