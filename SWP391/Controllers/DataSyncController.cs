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

        // Manual admin trigger for OpenAlex metadata ingestion.
        // Fetches, normalizes, validates, stores papers, then notifies matching followed topics/journals.
        [HttpPost("fetch-openalex")]
        public async Task<IActionResult> FetchOpenAlex(string keyword = "Computer Science", int maxResults = 20)
        {
            var result = await _dataSyncService.FetchOpenAlexAsync(keyword, maxResults);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // Backward-compatible route for existing clients.
        [HttpPost("sync-openalex")]
        public Task<IActionResult> SyncOpenAlex(string keyword = "Computer Science", int maxResults = 20)
        {
            return FetchOpenAlex(keyword, maxResults);
        }

        // Manual admin trigger for citation-count synchronization of already stored OpenAlex papers.
        [HttpPost("synchronize-citations")]
        public async Task<IActionResult> SynchronizeCitations(int maxPapers = 200)
        {
            var result = await _dataSyncService.SynchronizeOpenAlexCitationsAsync(maxPapers);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }
    }
}
