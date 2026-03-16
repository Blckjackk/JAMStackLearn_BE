using api_app.Services.Interfaces;

namespace api_app.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
    }

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
    }
}
