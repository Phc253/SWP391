using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Follow;
using SWP391.Service;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SWP391.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu người dùng đăng nhập
    public class FollowsController : ControllerBase
    {
        private readonly FollowService _followService;

        public FollowsController(FollowService followService)
        {
            _followService = followService;
        }

        // POST /api/follows/toggle — Theo dõi hoặc bỏ theo dõi tác giả
        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleFollow([FromBody] ToggleFollowRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var result = await _followService.ToggleFollowAsync(userId, request);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(new {
                success = true,
                isFollowed = result.Data,
                message = result.Data ? "Followed successfully." : "Unfollowed successfully."
            });
        }

        // GET /api/follows/my-follows — Lấy danh sách các đối tượng (tác giả) đang follow
        [HttpGet("my-follows")]
        public async Task<IActionResult> GetMyFollows()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var result = await _followService.GetUserFollowsAsync(userId);
            return Ok(new {
                success = true,
                data = result.Data
            });
        }
    }
}
