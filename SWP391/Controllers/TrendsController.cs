using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Service;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "IsMember")]
    public class TrendsController : ControllerBase
    {
        private readonly TrendService _trendService;

        public TrendsController(TrendService trendService)
        {
            _trendService = trendService;
        }

        [HttpGet("keyword")]
        public async Task<IActionResult> GetKeywordTrend([FromQuery] string keywordText)
        {
            if (string.IsNullOrWhiteSpace(keywordText)) return BadRequest("Keyword is required.");
            var result = await _trendService.GetKeywordTrendAsync(keywordText);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingTopics([FromQuery] int topN = 10)
        {
            var result = await _trendService.GetTrendingTopicsAsync(topN);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }

        [HttpGet("topic")]
        public async Task<IActionResult> GetTopicTrend([FromQuery] int? topicId, [FromQuery] string? topicName)
        {
            if (!topicId.HasValue && string.IsNullOrWhiteSpace(topicName))
                return BadRequest("Either topicId or topicName is required.");
            var result = await _trendService.GetTopicTrendAsync(topicId, topicName);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }

        [HttpGet("growth")]
        public async Task<IActionResult> GetKeywordGrowth([FromQuery] string keywordText, [FromQuery] int years = 5)
        {
            if (string.IsNullOrWhiteSpace(keywordText)) return BadRequest("Keyword is required.");
            var result = await _trendService.GetKeywordGrowthAsync(keywordText, years);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }

        [HttpGet("topic-growth")]
        public async Task<IActionResult> GetTopicGrowth([FromQuery] int? topicId, [FromQuery] string? topicName, [FromQuery] int years = 5)
        {
            if (!topicId.HasValue && string.IsNullOrWhiteSpace(topicName))
                return BadRequest("Either topicId or topicName is required.");
            var result = await _trendService.GetTopicGrowthAsync(topicId, topicName, years);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }

        // Now returns Momentum + CitationVelocity to match TrendSnapshot persistence.
        [HttpGet("activity-score")]
        public async Task<IActionResult> GetActivityScores([FromQuery] int topN = 10)
        {
            var result = await _trendService.GetActivityScoresAsync(topN);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }

        // Orchestrator — runs all three stages. Used by DataSyncService and TrendComputeBackgroundService.
        [HttpPost("compute-trends")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ComputeTrends()
        {
            var result = await _trendService.ComputeTrendsAsync();
            return result.Success ? Ok(result.Data) : StatusCode(500, result);
        }

        // ── Per-stage endpoints for demo / fine-grained control ──

        [HttpPost("compute/keywords")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ComputeKeywords()
        {
            var result = await _trendService.ComputeKeywordsStageAsync();
            return result.Success ? Ok(result.Data) : StatusCode(500, result);
        }

        [HttpPost("compute/topics")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ComputeTopics()
        {
            var result = await _trendService.ComputeTopicsStageAsync();
            return result.Success ? Ok(result.Data) : StatusCode(500, result);
        }

        [HttpPost("compute/snapshots")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ComputeSnapshots()
        {
            var result = await _trendService.ComputeSnapshotsStageAsync();
            return result.Success ? Ok(result.Data) : StatusCode(500, result);
        }

        [HttpGet("snapshot-history")]
        public async Task<IActionResult> GetSnapshotHistory([FromQuery] string keywordText, [FromQuery] int days = 30)
        {
            if (string.IsNullOrWhiteSpace(keywordText)) return BadRequest("keywordText is required.");
            var result = await _trendService.GetSnapshotHistoryAsync(keywordText, days);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }

        [HttpGet("topic-snapshot-history")]
        public async Task<IActionResult> GetTopicSnapshotHistory([FromQuery] int? topicId, [FromQuery] string? topicName, [FromQuery] int days = 30)
        {
            if (!topicId.HasValue && string.IsNullOrWhiteSpace(topicName))
                return BadRequest("Either topicId or topicName is required.");
            var result = await _trendService.GetTopicSnapshotHistoryAsync(topicId, topicName, days);
            return result.Success ? Ok(result.Data) : BadRequest(result);
        }
    }
}
