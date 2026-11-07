using ManagementSystem.Domain.Enums;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Domain.DTOs;

public sealed record TaskResponseDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    DateTimeOffset DueDate,
    TaskUserDto CreatedBy,
    TaskUserDto AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
