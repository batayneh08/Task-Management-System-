using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ManagementSystem.Infrastructure.Authentication;

public sealed class IdentityPasswordHasher : ManagementSystem.Application.Interfaces.IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
