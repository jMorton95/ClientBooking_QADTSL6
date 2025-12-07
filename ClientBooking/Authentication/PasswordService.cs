using ClientBooking.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace ClientBooking.Authentication;

public interface IPasswordService
{
    string HashPassword(User user, string password);
    bool CheckPassword(User user, string providedPassword, string hashedPassword);
}

public class PasswordService(PasswordHasher<User> passwordHasher) : IPasswordService
{
    //Compares a plaintext password against a hashed password.
    public string HashPassword(User user, string password)
    {
        return passwordHasher.HashPassword(user, password);
    }
    
    //Uses .NET's PBKDF2 implementation to verify a plaintext password against a hashed password.
    public bool CheckPassword(User user, string providedPassword, string hashedPassword)
    {
        var result = passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        
        return result 
            is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}