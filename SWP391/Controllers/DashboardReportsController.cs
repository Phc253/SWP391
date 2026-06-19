using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Dashboard;
using SWP391.Service;
using System.Security.Claims;

namespace SWP391.Controllers
{
    [Route("api/dashboard/reports")]
    [ApiController]
    [Authorize(Policy = "IsMember")]
    public class DashboardReportsController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardReportsController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST: api/dashboard/reports
        // Save a new report config for the current user.
        [HttpPost]
        public async Task<IActionResult> SaveReport([FromBody] SaveReportRequest request)
        {
            var result = await _dashboardService.SaveReportAsync(GetCurrentUserId(), request);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(nameof(GetReportById),
                new { id = result.Data!.ReportId }, result.Data);
        }

        // GET: api/dashboard/reports?page=1&pageSize=10
        // List the current user's saved reports, newest first.
        [HttpGet]
        public async Task<IActionResult> GetMyReports(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _dashboardService.GetMyReportsAsync(GetCurrentUserId(), page, pageSize);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(result.Data);
        }

        // GET: api/dashboard/reports/{id}
        // Get a single saved report. Returns 403 if the report belongs to another user.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(long id)
        {
            var result = await _dashboardService.GetReportByIdAsync(GetCurrentUserId(), id);
            if (!result.Success)
            {
                if (result.Error!.Contains("permission"))
                    return StatusCode(403, new { error = result.Error });
                if (result.Error.Contains("not found"))
                    return NotFound(new { error = result.Error });
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }

        // PUT: api/dashboard/reports/{id}
        // Replace name, type, and filterConfig. Returns 403 if not the owner.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReport(long id, [FromBody] SaveReportRequest request)
        {
            var result = await _dashboardService.UpdateReportAsync(GetCurrentUserId(), id, request);
            if (!result.Success)
            {
                if (result.Error!.Contains("permission"))
                    return StatusCode(403, new { error = result.Error });
                if (result.Error.Contains("not found"))
                    return NotFound(new { error = result.Error });
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }

        // DELETE: api/dashboard/reports/{id}
        // Delete a saved report. Returns 403 if not the owner.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(long id)
        {
            var result = await _dashboardService.DeleteReportAsync(GetCurrentUserId(), id);
            if (!result.Success)
            {
                if (result.Error!.Contains("permission"))
                    return StatusCode(403, new { error = result.Error });
                if (result.Error.Contains("not found"))
                    return NotFound(new { error = result.Error });
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Report deleted." });
        }
    }
}
