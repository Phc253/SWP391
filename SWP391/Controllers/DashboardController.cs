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
        // Returns a snapshot of system-wide metrics: totals, top keywords, papers by year,
        // and the last sync time. No authentication required — read-only public data.
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _dashboardService.GetSummaryAsync();
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }
    }
}
