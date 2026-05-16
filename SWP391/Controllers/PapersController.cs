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

        [HttpGet]
        public async Task<IActionResult> SearchPapers([FromQuery] string? keyword, [FromQuery] string? author, [FromQuery] string? journal, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var result = await _paperService.SearchPapersAsync(keyword, author, journal, page, pageSize);
            return Ok(result);
        }

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