using Microsoft.AspNetCore.Mvc;
using SWP391.Service;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SWP391.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorsController : ControllerBase
    {
        private readonly AuthorService _authorService;

        public AuthorsController(AuthorService authorService)
        {
            _authorService = authorService;
        }

        // GET /api/authors/{id} — Xem profile tác giả (public, không bắt buộc login nhưng nếu login sẽ hiển thị thêm IsFollowed)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuthorProfile(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
            {
                userId = parsedId;
            }

            var result = await _authorService.GetAuthorProfileAsync(id, userId);
            if (!result.Success)
            {
                return NotFound(new { success = false, message = result.Error });
            }

            return Ok(new { success = true, data = result.Data });
        }

        // GET /api/authors/search?name=... — Tìm kiếm tác giả theo tên trong DB local
        [HttpGet("search")]
        public async Task<IActionResult> SearchAuthors([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { success = false, message = "Name query parameter is required." });
            }

            var result = await _authorService.SearchAuthorsAsync(name);
            return Ok(new { success = true, data = result.Data });
        }
    }
}
