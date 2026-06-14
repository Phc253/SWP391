using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Service;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: api/reports/papers?page=1&pageSize=20&year=2023&keywordText=AI
        // Paginated paper report with optional year and keyword filters.
        [HttpGet("papers")]
        public async Task<IActionResult> GetPapersReport(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? year = null,
            [FromQuery] string? keywordText = null)
        {
            var result = await _reportService.GetPapersReportAsync(page, pageSize, year, keywordText);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // GET: api/reports/keyword-stats
        // Returns all keywords with total paper count, first year, and last year.
        [HttpGet("keyword-stats")]
        public async Task<IActionResult> GetKeywordStats()
        {
            var result = await _reportService.GetKeywordStatsAsync();
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // GET: api/reports/export/papers?year=2023&keywordText=AI
        // Downloads all matching papers as a CSV file (no pagination).
        [HttpGet("export/papers")]
        [Authorize(Policy = "IsMember")]
        public async Task<IActionResult> ExportPapersReport(
            [FromQuery] int? year = null,
            [FromQuery] string? keywordText = null)
        {
            var result = await _reportService.ExportPapersReportAsync(year, keywordText);
            if (!result.Success)
                return BadRequest(new { error = result.Error });
            var bytes = System.Text.Encoding.UTF8.GetBytes(result.Data!);
            return File(bytes, "text/csv", $"papers_report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        // GET: api/reports/export/keyword-stats
        // Downloads all keyword stats as a CSV file.
        [HttpGet("export/keyword-stats")]
        [Authorize(Policy = "IsMember")]
        public async Task<IActionResult> ExportKeywordStats()
        {
            var result = await _reportService.ExportKeywordStatsAsync();
            if (!result.Success)
                return StatusCode(500, new { error = result.Error });
            var bytes = System.Text.Encoding.UTF8.GetBytes(result.Data!);
            return File(bytes, "text/csv", $"keyword_stats_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
