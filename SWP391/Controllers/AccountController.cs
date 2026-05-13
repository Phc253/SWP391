using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Account;
using SWP391.Service;
using System.Security.Claims;

namespace SWP391.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AccountService _accountServices;

        public AccountController(AccountService accountServices)
        {
            _accountServices = accountServices;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _accountServices.RegisterAsync(request);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _accountServices.LoginAsync(request);
            if (!result.Success)
            {
                return Unauthorized(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        // [MỚI] Đây là API Test việc Protect tài nguyên bằng JWT
        [HttpGet("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize] // Bắt buộc phải có Token hợp lệ để chạy được
        public IActionResult GetProfile()
        {
            // Trong API này bạn có quyền đọc các Claims đã được giải mã mà hệ thống lấy được từ Token
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email);

            return Ok(new 
            {
                Message = "Nếu bạn thấy chữ này có nghĩa là Token của bạn hợp lệ!",
                UserId = userId,
                Email = email
            });
        }

        // [MỚI] Test Policy Based Authorization cho Admin
        [HttpGet("admin-dashboard")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] // Chỉ user có role Admin mới vào được
        public IActionResult GetAdminDashboard()
        {
            return Ok(new { Message = "Chào mừng Admin, đây là dữ liệu tuyệt mật." });
        }

        // [MỚI] Test Policy Based Authorization cho việc viết bài
        [HttpPost("publish-article")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CanPublishArticle")] // Admin hoặc Researcher
        public IActionResult PublishArticle()
        {
            return Ok(new { Message = "Bài báo đã đưa vào hàng chờ kiểm duyệt." });
        }
    }
}
