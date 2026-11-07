using ManagementSystem.Domain.DTOs;
using ManagementSystem.Domain.Entities;

namespace ManagementSystem.Application.Interfaces;

public interface ITokenGenerator
{
    AccessTokenDto GenerateToken(User user);
}
