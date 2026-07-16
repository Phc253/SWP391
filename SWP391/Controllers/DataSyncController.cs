using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Service;
using System.Security.Claims;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class DataSyncController : ControllerBase
    {
        private readonly DataSyncService _dataSyncService;

        public DataSyncController(DataSyncService dataSyncService)
        {
            _dataSyncService = dataSyncService;
        }

        // Manual sync trigger. Refreshes citation data for papers already stored locally.
        [HttpPost("sync-openalex")] 
        public async Task<IActionResult> SyncOpenAlex(int maxResults = 20)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = int.TryParse(userIdStr, out var id) ? id : null;

            var result = await _dataSyncService.SyncOpenAlexAsync(maxResults, userId);
            if (!result.Success)
            {
                if (result.Error != null && (result.Error.Contains("credit") || result.Error.Contains("budget") || result.Error.Contains("Quota") || result.Error.Contains("limit")))
                    return StatusCode(402, result);
                return StatusCode(500, result);
            }

            return Ok(result.Data);
        }

        // Manual fetch-only trigger. Fetches OpenAlex works by title/abstract search sorted by citation count.
        [HttpPost("/api/fetchdata/openalex")]
        public async Task<IActionResult> FetchOpenAlex(
            string keyword = "Computer Science",
            int maxResults = 20,
            bool useCheckpoint = false)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = int.TryParse(userIdStr, out var id) ? id : null;

            var result = await _dataSyncService.FetchOpenAlexAsync(keyword, maxResults, useCheckpoint, userId);
            if (!result.Success)
            {
                if (result.Error != null && (result.Error.Contains("credit") || result.Error.Contains("budget") || result.Error.Contains("Quota") || result.Error.Contains("limit")))
                    return StatusCode(402, result);
                return StatusCode(500, result);
            }

            return Ok(result.Data);
        }
    }
}
