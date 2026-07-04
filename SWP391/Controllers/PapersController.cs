using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Papers;
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
        public async Task<IActionResult> SearchPapers([FromQuery] PaperSearchRequest request)
        {
            var result = await _paperService.SearchPapersAsync(request);
            return Ok(result);
        }

        [HttpGet("facets/authors")]
        public async Task<IActionResult> GetAuthorFacets([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _paperService.GetAuthorFacetsAsync(q, page, pageSize);
            return Ok(result);
        }

        [HttpGet("facets/keywords")]
        public async Task<IActionResult> GetKeywordFacets([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _paperService.GetKeywordFacetsAsync(q, page, pageSize);
            return Ok(result);
        }

        [HttpGet("facets/topics")]
        public async Task<IActionResult> GetTopicFacets([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _paperService.GetTopicFacetsAsync(q, page, pageSize);
            return Ok(result);
        }

        [HttpGet("facets/topics/{topicId:int}/papers")]
        public async Task<IActionResult> GetPapersByTopic(int topicId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _paperService.GetPapersByTopicAsync(topicId, page, pageSize);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("facets/journals")]
        public async Task<IActionResult> GetJournalFacets([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _paperService.GetJournalFacetsAsync(q, page, pageSize);
            return Ok(result);
        }

        [HttpGet("facets/journals/{journalId:int}/papers")]
        public async Task<IActionResult> GetPapersByJournal(int journalId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _paperService.GetPapersByJournalAsync(journalId, page, pageSize);
            if (!result.Success)
            {
                return BadRequest(result);
            }

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
