namespace ManagementSystem.Domain.DTOs;

public sealed record AuthResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserDto User);
