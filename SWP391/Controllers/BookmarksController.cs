using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Bookmark;
using SWP391.Service;
using System.Security.Claims;

namespace SWP391.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires login
    public class BookmarksController : ControllerBase
    {
        private readonly BookmarkService _bookmarkService;

        public BookmarksController(BookmarkService bookmarkService)
        {
            _bookmarkService = bookmarkService;
        }

        // API Bật/tắt Bookmark (Nếu đã lưu thì hủy lưu, nếu chưa lưu thì thêm mới)
        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleBookmark([FromBody] ToggleBookmarkRequest request)
        {
            // Trích xuất UserId từ Token JWT của người dùng đang đăng nhập
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            // Gọi qua tầng Service để xử lý nghiệp vụ, không gọi trực tiếp Repository/Database ở đây
            var result = await _bookmarkService.ToggleBookmarkAsync(userId, request);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }
            
            return Ok(new { 
                success = true,
                isBookmarked = result.Data,
                message = result.Data ? "Bookmark added successfully." : "Bookmark removed successfully."
            });
        }

        // API Lấy danh sách toàn bộ Bookmarks của người dùng hiện tại
        [HttpGet("my-bookmarks")]
        public async Task<IActionResult> GetMyBookmarks()
        {
            // Trích xuất UserId từ Token JWT của người dùng đang đăng nhập
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var result = await _bookmarkService.GetUserBookmarksAsync(userId);
            return Ok(new {
                success = true,
                data = result.Data
            });
        }
    }
}