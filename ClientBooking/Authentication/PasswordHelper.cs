using System.Security.Cryptography;

namespace ClientBooking.Authentication;

public interface IPasswordHelper
{
    string HashPassword(string password);

    bool CheckPassword(string loginPassword, string storedPassword);
}

public class PasswordHelper(IPasswordHasher passwordHasher) : IPasswordHelper
{
    public string HashPassword(string password)
    {
        var rng = RandomNumberGenerator.Create();
        var salt = passwordHasher.CreateSalt();

        rng.GetBytes(salt);

        var passwordKey = passwordHasher.CreatePasswordKey(password, salt);

        var hashedPassword = passwordHasher.HashPassword(passwordKey, salt);
        
        return passwordHasher.HashedPasswordToString(hashedPassword);
    }

    public bool CheckPassword(string loginPassword, string storedPassword)
    {
        var bytes = passwordHasher.PasswordToBytes(storedPassword);

        var salt = passwordHasher.ExtractSalt(bytes);

        var passwordKey = passwordHasher.CreatePasswordKey(loginPassword, salt);

        var compared = passwordHasher.ComparePassword(passwordKey, salt, bytes);

        return compared;
    }
}