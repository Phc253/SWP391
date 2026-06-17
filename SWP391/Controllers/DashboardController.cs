using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Service;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // GET: api/dashboard/summary
        // System-wide totals + snapshot-driven TrendingKeywords. No authentication required.
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _dashboardService.GetSummaryAsync();
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // GET: api/dashboard/me
        // Personalized dashboard: bookmarks/follows breakdown, new papers from followed journals
        // (last 30 days by ingestion time, not publication year), trending limited to followed
        // topics, recent notifications, and bookmarked papers by year.
        [HttpGet("me")]
        [Authorize(Policy = "IsMember")]
        public async Task<IActionResult> GetMyDashboard()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Invalid token: missing user id." });

            var result = await _dashboardService.GetUserSummaryAsync(userId);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }
    }
}
