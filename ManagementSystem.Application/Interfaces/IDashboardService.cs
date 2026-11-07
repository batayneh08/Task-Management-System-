using ManagementSystem.Domain.DTOs;

namespace ManagementSystem.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
