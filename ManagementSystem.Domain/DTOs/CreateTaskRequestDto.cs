using System.ComponentModel.DataAnnotations;
using ManagementSystem.Domain.Enums;

namespace ManagementSystem.Domain.DTOs;

public sealed class CreateTaskRequestDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    [Required]
    public DateTimeOffset DueDate { get; init; }

    [Required]
    public Guid AssignedToId { get; init; }
}
