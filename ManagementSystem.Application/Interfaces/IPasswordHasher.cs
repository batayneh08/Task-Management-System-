using ManagementSystem.Domain.Entities;

namespace ManagementSystem.Application.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}
