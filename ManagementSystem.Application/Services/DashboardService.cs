using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.DTOs;
using ManagementSystem.Domain.Entities;

namespace ManagementSystem.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private const int DashboardTaskLimit = 5;

    private readonly ITaskRepository _taskRepository;

    public DashboardService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<DashboardResponseDto> GetDashboardAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var counts = await _taskRepository.GetCountsAsync(cancellationToken);
        var recentTasks = await _taskRepository.GetRecentAsync(DashboardTaskLimit, cancellationToken);
        var highPriorityTasks = await _taskRepository.GetHighPriorityAsync(DashboardTaskLimit, cancellationToken);
        var myAssignedTasks = await _taskRepository.GetAssignedToUserAsync(
            currentUserId,
            DashboardTaskLimit,
            cancellationToken);

        return new DashboardResponseDto(
            counts,
            MapTasks(recentTasks),
            MapTasks(highPriorityTasks),
            MapTasks(myAssignedTasks));
    }

    private static IReadOnlyList<TaskResponseDto> MapTasks(IEnumerable<TaskItem> tasks)
    {
        return tasks.Select(task => new TaskResponseDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            new TaskUserDto(task.CreatedBy.Id, task.CreatedBy.FullName),
            new TaskUserDto(task.AssignedTo.Id, task.AssignedTo.FullName),
            task.CreatedAt,
            task.UpdatedAt)).ToList();
    }
}
