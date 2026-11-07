using ManagementSystem.Domain.Entities;

namespace ManagementSystem.Application.Interfaces;

public interface IActivityLogRepository
{
    Task<IReadOnlyList<ActivityLog>> GetByTaskIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ActivityLog activityLog, CancellationToken cancellationToken = default);
}
