using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Models;
using SWP391.Service;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataSyncController : ControllerBase
    {
        private readonly AcademicDataIntegrationService _integrationService;

        public DataSyncController(AcademicDataIntegrationService integrationService)
        {
            _integrationService = integrationService;
        }

        // Tạm thời endpoint không yêu cầu Admin hoặc Authorize cứng để bạn dễ test
        // Sau này có thiết lập role hoàn chỉnh có thể mở lại: [Authorize(Roles = "Admin")]
        [HttpPost("sync-openalex")]
        public async Task<IActionResult> SyncOpenAlex(string keyword = "Computer Science", int maxResults = 20)
        {
            try
            {
                var resultCount = await _integrationService.FetchAndSaveDataFromOpenAlexAsync(keyword, maxResults);
                return Ok(ServiceResult<string>.Ok($"Successfully fetched and saved {resultCount} papers."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<string>.Fail($"Error syncing data: {ex.Message}"));
            }
        }
    }
}