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

        // Manual admin trigger for the OpenAlex data acquisition pipeline.
        // Creates a SyncJob, fetches metadata, stores normalized records, then refreshes trend data.
        [HttpPost("sync-openalex")] 
        public async Task<IActionResult> SyncOpenAlex(string keyword = "Computer Science", int maxResults = 20)
        {
            var result = await _dataSyncService.SyncOpenAlexAsync   (keyword, maxResults);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }
    }
}
