using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Service;

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
            var result = await _dataSyncService.SyncOpenAlexAsync(maxResults);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // Manual fetch-only trigger. Fetches current-year OpenAlex works sorted by citation count.
        [HttpPost("/api/fetchdata/openalex")]
        public async Task<IActionResult> FetchOpenAlex(
            string keyword = "Computer Science",
            int maxResults = 20,
            bool useCheckpoint = false)
        {
            var result = await _dataSyncService.FetchOpenAlexAsync(keyword, maxResults, useCheckpoint);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }
    }
}
