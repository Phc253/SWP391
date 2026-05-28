using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Service;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrendsController : ControllerBase
    {
        private readonly TrendService _trendService;

        public TrendsController(TrendService trendService)
        {
            _trendService = trendService;
        }

        // GET: api/Trends/keyword?keywordText=AI
        [HttpGet("keyword")]
        public async Task<IActionResult> GetKeywordTrend([FromQuery] string keywordText)
        {
            if (string.IsNullOrWhiteSpace(keywordText))
                return BadRequest("Keyword is required.");

            var result = await _trendService.GetKeywordTrendAsync(keywordText);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data); 
        }

        // GET: api/Trends/trending?topN=10
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingTopics([FromQuery] int topN = 10)
        {
            var result = await _trendService.GetTrendingTopicsAsync(topN);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // ── NEW ENDPOINTS BELOW ───────────────────────────────────────────────────────

        // GET: api/trends/topic?topicName=MachineLearning
        // Returns a year-by-year publication chart for a ResearchTopic.
        // Papers shared across keywords under the same topic are counted only once.
        [HttpGet("topic")]
        public async Task<IActionResult> GetTopicTrend([FromQuery] string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return BadRequest("Topic name is required.");

            var result = await _trendService.GetTopicTrendAsync(topicName);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // GET: api/trends/growth?keywordText=AI&years=5
        // Returns a complete year-by-year growth rate series for the requested keyword.
        // Years with no papers are zero-filled; GrowthRate is null for the first year in the window.
        [HttpGet("growth")]
        public async Task<IActionResult> GetKeywordGrowth(
            [FromQuery] string keywordText,
            [FromQuery] int years = 5)
        {
            if (string.IsNullOrWhiteSpace(keywordText))
                return BadRequest("Keyword is required.");

            var result = await _trendService.GetKeywordGrowthAsync(keywordText, years);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // GET: api/trends/topic-growth?topicName=AI&years=5
        // Same as /growth but scoped to a ResearchTopic (aggregates all its keywords).
        [HttpGet("topic-growth")]
        public async Task<IActionResult> GetTopicGrowth(
            [FromQuery] string topicName,
            [FromQuery] int years = 5)
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return BadRequest("Topic name is required.");

            var result = await _trendService.GetTopicGrowthAsync(topicName, years);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // GET: api/trends/activity-score?topN=10
        // Returns keywords ranked by a composite Research Activity Score (0–100).
        // Score = (RecentPaperCount * 1.0 + GrowthRate * 0.5) normalized to the highest scorer.
        [HttpGet("activity-score")]
        public async Task<IActionResult> GetActivityScores([FromQuery] int topN = 10)
        {
            var result = await _trendService.GetActivityScoresAsync(topN);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // POST: api/trends/compute-trends  [AdminOnly]
        // Materializes live paper counts into the PublicationTrends cache table.
        // Run this after a data sync to refresh cached trend data.
        // Returns a 500 on failure (it is a server-side operation, not a client input error).
        [HttpPost("compute-trends")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ComputeTrends()
        {
            var result = await _trendService.ComputeTrendsAsync();
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }
    }
}
