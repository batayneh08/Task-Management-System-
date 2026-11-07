using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.Entities;
using ManagementSystem.Domain.DTOs;
using ManagementSystem.Domain.Enums;
using ManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Infrastructure.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly TaskManagementDbContext _dbContext;

    public TaskRepository(TaskManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private DbSet<TaskItem> Tasks => _dbContext.Tasks;

    public Task<TaskItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Tasks.FindAsync([id], cancellationToken).AsTask();
    }

    public Task<TaskItem?> GetByIdWithUsersAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Tasks
            .Include(task => task.CreatedBy)
            .Include(task => task.AssignedTo)
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetFilteredAsync(
        TaskStatus? status = null,
        TaskPriority? priority = null,
        Guid? assignedToId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TaskItem> query = Tasks
            .AsNoTracking()
            .Include(task => task.CreatedBy)
            .Include(task => task.AssignedTo);

        if (status.HasValue)
        {
            query = query.Where(task => task.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(task => task.Priority == priority.Value);
        }

        if (assignedToId.HasValue)
        {
            query = query.Where(task => task.AssignedToId == assignedToId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search.Trim()}%";
            query = query.Where(task => EF.Functions.ILike(task.Title, searchPattern));
        }

        return await query
            .OrderBy(task => task.DueDate)
            .ThenByDescending(task => task.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await Tasks.AddAsync(task, cancellationToken);
    }

    public void Update(TaskItem task)
    {
        Tasks.Update(task);
    }

    public void Delete(TaskItem task)
    {
        Tasks.Remove(task);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskCountsDto> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        return new TaskCountsDto(
            await Tasks.CountAsync(cancellationToken),
            await Tasks.CountAsync(task => task.Status == TaskStatus.ToDo, cancellationToken),
            await Tasks.CountAsync(task => task.Status == TaskStatus.InProgress, cancellationToken),
            await Tasks.CountAsync(task => task.Status == TaskStatus.Completed, cancellationToken));
    }

    public async Task<IReadOnlyList<TaskItem>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await GetDashboardTasks()
            .OrderByDescending(task => task.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetHighPriorityAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await GetDashboardTasks()
            .Where(task => task.Priority == TaskPriority.High)
            .OrderBy(task => task.DueDate)
            .ThenByDescending(task => task.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await GetDashboardTasks()
            .Where(task => task.AssignedToId == userId)
            .OrderBy(task => task.DueDate)
            .ThenByDescending(task => task.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<TaskItem> GetDashboardTasks()
    {
        return Tasks
            .AsNoTracking()
            .Include(task => task.CreatedBy)
            .Include(task => task.AssignedTo);
    }
}
