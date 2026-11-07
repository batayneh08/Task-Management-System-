using ManagementSystem.Domain.DTOs;

namespace ManagementSystem.Application.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponseDto>> GetTasksAsync(
        TaskFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto> GetTaskByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityLogResponseDto>> GetTaskActivityAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto> CreateTaskAsync(
        CreateTaskRequestDto request,
        Guid createdById,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto> UpdateTaskAsync(
        Guid id,
        UpdateTaskRequestDto request,
        Guid updatedById,
        CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(
        Guid id,
        Guid deletedById,
        CancellationToken cancellationToken = default);
}
