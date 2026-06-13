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
    }
}
