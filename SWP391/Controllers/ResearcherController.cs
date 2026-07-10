using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Researcher;
using SWP391.Service;

namespace SWP391.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "CanUseResearcherAnalytics")]
public class ResearcherController : ControllerBase
{
    private readonly ResearcherService _researcherService;

    public ResearcherController(ResearcherService researcherService)
    {
        _researcherService = researcherService;
    }

    // GET: api/researcher/dashboard?years=5&topN=10
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int years = 5,
        [FromQuery] int topN = 10)
    {
        var result = await _researcherService.GetDashboardAsync(years, topN);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // GET: api/researcher/discovery/emerging-topics?years=5&topN=10
    [HttpGet("discovery/emerging-topics")]
    public async Task<IActionResult> GetEmergingTopics(
        [FromQuery] int years = 5,
        [FromQuery] int topN = 10)
    {
        var result = await _researcherService.GetEmergingTopicsAsync(years, topN);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // GET: api/researcher/discovery/related-topics?topicId=1&topN=10
    // GET: api/researcher/discovery/related-topics?keywordText=machine learning&topN=10
    [HttpGet("discovery/related-topics")]
    public async Task<IActionResult> GetRelatedTopics(
        [FromQuery] int? topicId = null,
        [FromQuery] string? keywordText = null,
        [FromQuery] int topN = 10)
    {
        var result = await _researcherService.GetRelatedTopicsAsync(topicId, keywordText, topN);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // GET: api/researcher/compare/keywords?left=AI&right=Blockchain&years=5
    // left/right accept either id or exact keyword text.
    [HttpGet("compare/keywords")]
    public async Task<IActionResult> CompareKeywords(
        [FromQuery] string left,
        [FromQuery] string right,
        [FromQuery] int years = 5)
    {
        var result = await _researcherService.CompareKeywordsAsync(left, right, years);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // GET: api/researcher/compare/topics?left=AI&right=Data Science&years=5
    // left/right accept either id or exact topic name.
    [HttpGet("compare/topics")]
    public async Task<IActionResult> CompareTopics(
        [FromQuery] string left,
        [FromQuery] string right,
        [FromQuery] int years = 5)
    {
        var result = await _researcherService.CompareTopicsAsync(left, right, years);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // GET: api/researcher/watchlist
    [HttpGet("watchlist")]
    public async Task<IActionResult> GetWatchlist()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { error = "Invalid token: missing user id." });
        }

        var result = await _researcherService.GetWatchlistAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // POST: api/researcher/watchlist
    // Body: { "targetId": 1, "targetType": "Keyword" }
    [HttpPost("watchlist")]
    public async Task<IActionResult> AddToWatchlist([FromBody] ResearcherWatchlistRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { error = "Invalid token: missing user id." });
        }

        var result = await _researcherService.AddToWatchlistAsync(userId, request);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // DELETE: api/researcher/watchlist/Keyword/1
    [HttpDelete("watchlist/{targetType}/{targetId:long}")]
    public async Task<IActionResult> RemoveFromWatchlist(string targetType, long targetId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { error = "Invalid token: missing user id." });
        }

        var result = await _researcherService.RemoveFromWatchlistAsync(userId, targetId, targetType);
        return result.Success ? Ok(new { message = "Watchlist item removed." }) : BadRequest(result);
    }

    // GET: api/researcher/reports/publication-trend?targetType=Keyword&target=AI&years=5
    [HttpGet("reports/publication-trend")]
    public async Task<IActionResult> GetPublicationTrendReport(
        [FromQuery] string targetType,
        [FromQuery] string target,
        [FromQuery] int years = 5)
    {
        var result = await _researcherService.GetPublicationTrendReportAsync(targetType, target, years);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // GET: api/researcher/reports/keyword-comparison?left=AI&right=Blockchain&years=5
    [HttpGet("reports/keyword-comparison")]
    public async Task<IActionResult> GetKeywordComparisonReport(
        [FromQuery] string left,
        [FromQuery] string right,
        [FromQuery] int years = 5)
    {
        var result = await _researcherService.GetKeywordComparisonReportAsync(left, right, years);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    // GET: api/researcher/reports/journal-summary?journalId=1&years=5
    // GET: api/researcher/reports/journal-summary?journalName=Nature&years=5
    [HttpGet("reports/journal-summary")]
    public async Task<IActionResult> GetJournalSummaryReport(
        [FromQuery] int? journalId = null,
        [FromQuery] string? journalName = null,
        [FromQuery] int years = 5)
    {
        var result = await _researcherService.GetJournalSummaryReportAsync(journalId, journalName, years);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }
}
