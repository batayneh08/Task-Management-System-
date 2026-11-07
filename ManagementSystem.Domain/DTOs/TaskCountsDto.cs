namespace ManagementSystem.Domain.DTOs;

public sealed record TaskCountsDto(
    int TotalTasks,
    int ToDoTasks,
    int InProgressTasks,
    int CompletedTasks);
