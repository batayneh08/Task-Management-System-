namespace ManagementSystem.Domain.Entities;

public sealed class ActivityLog
{
    public Guid Id { get; set; }

    public Guid? TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Action { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
