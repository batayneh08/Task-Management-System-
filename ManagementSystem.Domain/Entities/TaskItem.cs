using ManagementSystem.Domain.Enums;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Domain.Entities;

public sealed class TaskItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTimeOffset DueDate { get; set; }

    public Guid CreatedById { get; set; }

    public User CreatedBy { get; set; } = null!;

    public Guid AssignedToId { get; set; }

    public User AssignedTo { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
