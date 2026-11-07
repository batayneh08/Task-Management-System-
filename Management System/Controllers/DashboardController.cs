using System.Security.Claims;
using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Management_System.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType<DashboardResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claimValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var dashboard = await _dashboardService.GetDashboardAsync(currentUserId, cancellationToken);
        return Ok(dashboard);
    }
}
