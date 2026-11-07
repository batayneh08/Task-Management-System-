using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.Entities;
using ManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementSystem.Infrastructure.Repositories;

public sealed class ActivityLogRepository : IActivityLogRepository
{
    private readonly TaskManagementDbContext _dbContext;

    public ActivityLogRepository(TaskManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ActivityLog>> GetByTaskIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ActivityLogs
            .AsNoTracking()
            .Include(activity => activity.User)
            .Where(activity => activity.TaskId == taskId)
            .OrderByDescending(activity => activity.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        ActivityLog activityLog,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ActivityLogs.AddAsync(activityLog, cancellationToken);
    }
}
