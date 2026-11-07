using System.ComponentModel.DataAnnotations;
using ManagementSystem.Domain.Enums;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Domain.DTOs;

public sealed class UpdateTaskRequestDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    public TaskStatus Status { get; init; } = TaskStatus.ToDo;

    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    [Required]
    public DateTimeOffset DueDate { get; init; }

    [Required]
    public Guid AssignedToId { get; init; }

    [StringLength(4000)]
    public string? ProgressSummary { get; init; }
}
