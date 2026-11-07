using System.Text.Json;
using ManagementSystem.Application.Exceptions;
using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.DTOs;
using ManagementSystem.Domain.Entities;
using ManagementSystem.Domain.Enums;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public TaskService(
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        IActivityLogRepository activityLogRepository)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<IReadOnlyList<TaskResponseDto>> GetTasksAsync(
        TaskFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ValidateFilter(filter);

        var tasks = await _taskRepository.GetFilteredAsync(
            filter.Status,
            filter.Priority,
            filter.AssignedToId,
            filter.Search,
            cancellationToken);

        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<TaskResponseDto> GetTaskByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdWithUsersAsync(id, cancellationToken);
        if (task is null)
        {
            throw new ResourceNotFoundException("Task");
        }

        return MapToResponse(task);
    }

    public async Task<TaskResponseDto> CreateTaskAsync(
        CreateTaskRequestDto request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        ValidateTitle(request.Title);
        ValidateDescription(request.Description);
        ValidateDueDate(request.DueDate);
        ValidatePriority(request.Priority);

        if (createdById == Guid.Empty || request.AssignedToId == Guid.Empty)
        {
            throw new RequestValidationException("A valid creator and assigned user are required.");
        }

        var creator = await _userRepository.GetByIdAsync(createdById, cancellationToken);
        if (creator is null)
        {
            throw new ResourceNotFoundException("Creating user");
        }

        var assignedUser = await _userRepository.GetByIdAsync(request.AssignedToId, cancellationToken);
        if (assignedUser is null)
        {
            throw new ResourceNotFoundException("Assigned user");
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description),
            Status = TaskStatus.ToDo,
            Priority = request.Priority,
            DueDate = request.DueDate,
            CreatedById = creator.Id,
            CreatedBy = creator,
            AssignedToId = assignedUser.Id,
            AssignedTo = assignedUser
        };

        await _taskRepository.AddAsync(task, cancellationToken);
        await _activityLogRepository.AddAsync(
            new ActivityLog
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                Task = task,
                TaskTitle = task.Title,
                UserId = creator.Id,
                User = creator,
                Action = "Created",
                NewValue = JsonSerializer.Serialize(new
                {
                    task.Title,
                    task.Status,
                    task.Priority,
                    task.DueDate,
                    task.AssignedToId
                })
            },
            cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task<TaskResponseDto> UpdateTaskAsync(
        Guid id,
        UpdateTaskRequestDto request,
        Guid updatedById,
        CancellationToken cancellationToken = default)
    {
        ValidateTitle(request.Title);
        ValidateDescription(request.Description);
        ValidateDueDate(request.DueDate);
        ValidateStatus(request.Status);
        ValidatePriority(request.Priority);

        if (request.AssignedToId == Guid.Empty)
        {
            throw new RequestValidationException("A valid assigned user is required.");
        }

        var task = await _taskRepository.GetByIdWithUsersAsync(id, cancellationToken);
        if (task is null)
        {
            throw new ResourceNotFoundException("Task");
        }

        var actingUser = await _userRepository.GetByIdAsync(updatedById, cancellationToken);
        if (actingUser is null)
        {
            throw new ResourceNotFoundException("Updating user");
        }

        var normalizedTitle = request.Title.Trim();
        var normalizedDescription = NormalizeDescription(request.Description);
        var isAdmin = actingUser.Role == UserRole.Admin;
        if (!isAdmin && task.AssignedToId != actingUser.Id)
        {
            throw new ForbiddenOperationException("Only the Admin or assigned user can update this task.");
        }

        var changesAdminManagedDetails =
            task.Title != normalizedTitle ||
            task.Description != normalizedDescription ||
            task.Priority != request.Priority ||
            task.DueDate != request.DueDate ||
            task.AssignedToId != request.AssignedToId;

        if (!isAdmin && changesAdminManagedDetails)
        {
            throw new ForbiddenOperationException(
                "Only an Admin can change the task title, description, priority, due date, or assignee.");
        }

        var assignedUser = await _userRepository.GetByIdAsync(request.AssignedToId, cancellationToken);
        if (assignedUser is null)
        {
            throw new ResourceNotFoundException("Assigned user");
        }

        var previousStatus = task.Status;
        var changes = new List<ActivityFieldChange>();
        AddChangeLog(changes, "Title", task.Title, normalizedTitle);
        AddChangeLog(changes, "Description", task.Description, normalizedDescription);
        AddChangeLog(changes, "Status", task.Status.ToString(), request.Status.ToString());
        AddChangeLog(changes, "Priority", task.Priority.ToString(), request.Priority.ToString());
        AddChangeLog(changes, "DueDate", task.DueDate.ToString("O"), request.DueDate.ToString("O"));
        AddChangeLog(changes, "Assignee", task.AssignedTo.FullName, assignedUser.FullName);

        task.Title = normalizedTitle;
        task.Description = normalizedDescription;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.AssignedToId = assignedUser.Id;
        task.AssignedTo = assignedUser;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        var progressSummary = request.ProgressSummary?.Trim();
        if (!isAdmin)
        {
            var statusChanged = previousStatus != request.Status;
            if (statusChanged || !string.IsNullOrWhiteSpace(progressSummary))
            {
                await _activityLogRepository.AddAsync(
                    new ActivityLog
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        Task = task,
                        TaskTitle = task.Title,
                        UserId = actingUser.Id,
                        User = actingUser,
                        Action = "Updated Status",
                        OldValue = previousStatus.ToString(),
                        NewValue = string.IsNullOrWhiteSpace(progressSummary)
                            ? request.Status.ToString()
                            : JsonSerializer.Serialize(new
                            {
                                Value = request.Status.ToString(),
                                Summary = progressSummary
                            })
                    },
                    cancellationToken);
            }
        }
        else
        {
            foreach (var change in changes)
            {
                if (change.OldValue == change.NewValue)
                {
                    continue;
                }

                await _activityLogRepository.AddAsync(
                    new ActivityLog
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        Task = task,
                        TaskTitle = task.Title,
                        UserId = actingUser.Id,
                        User = actingUser,
                        Action = $"Updated {change.Field}",
                        OldValue = change.OldValue,
                        NewValue = change.NewValue
                    },
                    cancellationToken);
            }
        }

        _taskRepository.Update(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task DeleteTaskAsync(
        Guid id,
        Guid deletedById,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            throw new ResourceNotFoundException("Task");
        }

        var actingUser = await _userRepository.GetByIdAsync(deletedById, cancellationToken);
        if (actingUser is null)
        {
            throw new ResourceNotFoundException("Deleting user");
        }

        await _activityLogRepository.AddAsync(
            new ActivityLog
            {
                Id = Guid.NewGuid(),
                TaskId = null,
                TaskTitle = task.Title,
                UserId = actingUser.Id,
                User = actingUser,
                Action = "Deleted",
                OldValue = JsonSerializer.Serialize(new
                {
                    task.Title,
                    task.Description,
                    task.Status,
                    task.Priority,
                    task.DueDate,
                    task.AssignedToId
                })
            },
            cancellationToken);

        _taskRepository.Delete(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActivityLogResponseDto>> GetTaskActivityAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            throw new ResourceNotFoundException("Task");
        }

        var activityLogs = await _activityLogRepository.GetByTaskIdAsync(taskId, cancellationToken);
        return activityLogs.Select(activity => new ActivityLogResponseDto(
            activity.Id,
            activity.TaskId,
            activity.TaskTitle,
            activity.UserId,
            activity.User.FullName,
            activity.Action,
            activity.OldValue,
            activity.NewValue,
            activity.CreatedAt)).ToList();
    }

    private static void AddChangeLog(
        ICollection<ActivityFieldChange> changes,
        string fieldName,
        string? oldValue,
        string? newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        changes.Add(new ActivityFieldChange(fieldName, oldValue, newValue));
    }

    private sealed record ActivityFieldChange(string Field, string? OldValue, string? NewValue);

    private static TaskResponseDto MapToResponse(TaskItem task)
    {
        return new TaskResponseDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            new TaskUserDto(task.CreatedBy.Id, task.CreatedBy.FullName),
            new TaskUserDto(task.AssignedTo.Id, task.AssignedTo.FullName),
            task.CreatedAt,
            task.UpdatedAt);
    }

    private static void ValidateFilter(TaskFilterDto filter)
    {
        if (filter.Status.HasValue)
        {
            ValidateStatus(filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            ValidatePriority(filter.Priority.Value);
        }
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
        {
            throw new RequestValidationException("Task title is required and must not exceed 200 characters.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description?.Length > 4000)
        {
            throw new RequestValidationException("Task description must not exceed 4000 characters.");
        }
    }

    private static void ValidateDueDate(DateTimeOffset dueDate)
    {
        if (dueDate == default)
        {
            throw new RequestValidationException("A due date is required.");
        }
    }

    private static void ValidateStatus(TaskStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new RequestValidationException("Task status is invalid.");
        }
    }

    private static void ValidatePriority(TaskPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new RequestValidationException("Task priority is invalid.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
