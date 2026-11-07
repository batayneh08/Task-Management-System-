using ManagementSystem.Domain.Enums;

namespace ManagementSystem.Domain.DTOs;

public sealed record AuthenticatedUserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role);
