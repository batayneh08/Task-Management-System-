using ManagementSystem.Domain.Entities;
using ManagementSystem.Domain.Enums;
using ManagementSystem.Domain.DTOs;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskItem?> GetByIdWithUsersAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    void Update(TaskItem task);

    void Delete(TaskItem task);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetFilteredAsync(
        TaskStatus? status = null,
        TaskPriority? priority = null,
        Guid? assignedToId = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<TaskCountsDto> GetCountsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetHighPriorityAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);
}
