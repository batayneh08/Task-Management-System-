using ManagementSystem.Domain.Enums;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Domain.DTOs;

public sealed class TaskFilterDto
{
    public TaskStatus? Status { get; init; }

    public TaskPriority? Priority { get; init; }

    public Guid? AssignedToId { get; init; }

    public string? Search { get; init; }
}
