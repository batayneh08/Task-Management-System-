using ManagementSystem.Domain.DTOs;

namespace ManagementSystem.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponseDto>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<UserResponseDto> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
