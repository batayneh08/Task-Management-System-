namespace ManagementSystem.Domain.DTOs;

public sealed record DashboardResponseDto(
    TaskCountsDto Counts,
    IReadOnlyList<TaskResponseDto> RecentTasks,
    IReadOnlyList<TaskResponseDto> HighPriorityTasks,
    IReadOnlyList<TaskResponseDto> MyAssignedTasks);
