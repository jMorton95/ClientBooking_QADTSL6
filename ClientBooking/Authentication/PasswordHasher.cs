using System.Security.Cryptography;
using System.Text;

namespace ClientBooking.Authentication;


public interface IPasswordHasher
{
    byte[] HashPassword(Rfc2898DeriveBytes passwordKey, byte[] salt);
    string HashedPasswordToString(byte[] bytePassword);
    byte[] CreateSalt() => new byte[32];
    bool ComparePassword(Rfc2898DeriveBytes passwordKey, byte[] salt, byte[] bytes);

    byte[] ExtractSalt(byte[] passwordBytes);

    byte[] PasswordToBytes(string storedPassword);

    Rfc2898DeriveBytes CreatePasswordKey(string loginPassword, byte[] salt);
}

public class PasswordHasher : IPasswordHasher
{
    /// Create an empty byte 32 array
    public byte[] CreateSalt() => new byte[32];

    
    /// Merges the passwordKey and salt into a fully hashed password byte array 
    public byte[] HashPassword(Rfc2898DeriveBytes passwordKey, byte[] salt)
    {
        var passwordBytes = passwordKey.GetBytes(32);
        var hashedPassword = new byte[salt.Length + passwordBytes.Length];
        
        Array.Copy(salt, 0, hashedPassword, 0, salt.Length);
        
        Array.Copy(passwordBytes, 0, hashedPassword, salt.Length, passwordBytes.Length);

        return hashedPassword;
    }

    
    //Concatenates a hashed password byte array into a single string
    public string HashedPasswordToString(byte[] bytePassword)
    {
        var builder = new StringBuilder(bytePassword.Length * 2);
        
        foreach (var b in bytePassword)
        {
            builder.Append(b.ToString("x2"));
        }
        
        return builder.ToString();
    }
    
    //Deconstructs a password string into a byte array of length 64
    public byte[] PasswordToBytes(string storedPassword)
    {
        var bytes = new byte[64]; 
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(storedPassword.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    //Extracts the salt bytes from hashed password converted to byte array.
    public byte[] ExtractSalt(byte[] passwordBytes)
    {
        var salt = CreateSalt();
        Array.Copy(passwordBytes, 0, salt, 0, salt.Length);

        return salt;
    }

    //Creates a password key from a plaintext password and a pre-generated salt
    public Rfc2898DeriveBytes CreatePasswordKey(string loginPassword, byte[] salt)
    {
        return new Rfc2898DeriveBytes(loginPassword, salt, 100000, HashAlgorithmName.SHA256);
    }

    //Directly compares the bytes of a password key and password bytes
    //This uses the length of the salt to determine exactly were in the hashed password to begin comparison.
    public bool ComparePassword(Rfc2898DeriveBytes passwordKey, byte[] salt, byte[] passwordBytes)
    {
        var hashBytes = passwordKey.GetBytes(32);
        
        for (var i = 0; i < hashBytes.Length; i++)
        {
            if (hashBytes[i] != passwordBytes[i + salt.Length])
            {
                return false; 
            }
        }

        return true;
    }
}