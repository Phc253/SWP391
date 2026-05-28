using Microsoft.AspNetCore.Mvc;
using SWP391.Service;

namespace SWP391.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PapersController : ControllerBase
    {
        private readonly PaperService _paperService;

        public PapersController(PaperService paperService)
        {
            _paperService = paperService;
        }

        // API Tìm kiếm bài báo: hỗ trợ tìm theo keyword, author, journal và hỗ trợ phân trang (pagination)
        [HttpGet]
        public async Task<IActionResult> SearchPapers([FromQuery] string? keyword, [FromQuery] string? author, [FromQuery] string? journal, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // Validate phân trang cơ bản tránh dữ liệu xấu (page <= 0 sẽ default về 1)
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var result = await _paperService.SearchPapersAsync(keyword, author, journal, page, pageSize);
            return Ok(result);
        }

        // API Xem chi tiết bài báo theo ID trên DB (Trả ra cả Title, Abstract, Tác giả, và tạp chí)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaperDetails(long id)
        {
            var result = await _paperService.GetPaperDetailsAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }
}