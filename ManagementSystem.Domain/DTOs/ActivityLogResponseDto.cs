namespace ManagementSystem.Domain.DTOs;

public sealed record ActivityLogResponseDto(
    Guid Id,
    Guid? TaskId,
    string TaskTitle,
    Guid UserId,
    string UserFullName,
    string Action,
    string? OldValue,
    string? NewValue,
    DateTimeOffset CreatedAt);
