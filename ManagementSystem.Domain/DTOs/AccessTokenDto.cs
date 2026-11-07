namespace ManagementSystem.Domain.DTOs;

public sealed record AccessTokenDto(string Token, DateTimeOffset ExpiresAt);
