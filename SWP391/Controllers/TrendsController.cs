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
    }
}
