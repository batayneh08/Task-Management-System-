using ManagementSystem.Application.Exceptions;
using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.DTOs;
using ManagementSystem.Domain.Entities;

namespace ManagementSystem.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserResponseDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToResponse).ToList();
    }

    public async Task<UserResponseDto> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new ResourceNotFoundException("User");
        }

        return MapToResponse(user);
    }

    private static UserResponseDto MapToResponse(User user)
    {
        return new UserResponseDto(user.Id, user.FullName, user.Email, user.Role, user.CreatedAt);
    }
}
