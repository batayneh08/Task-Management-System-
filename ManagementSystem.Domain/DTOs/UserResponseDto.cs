using ManagementSystem.Domain.Enums;

namespace ManagementSystem.Domain.DTOs;

public sealed record UserResponseDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    DateTimeOffset CreatedAt);
