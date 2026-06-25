using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Account;
using SWP391.Extensions;
using SWP391.Models.Common;
using SWP391.Service;
using System.Security.Claims;

namespace SWP391.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AccountService _accountServices;
        private readonly ActivityLogService _activityLogService;

        public AccountController(AccountService accountServices, ActivityLogService activityLogService)
        {
            _accountServices = accountServices;
            _activityLogService = activityLogService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _accountServices.RegisterAsync(request);
            if (!result.Success)
            {
                return this.ToErrorResult(result);
            }

            return Ok(result.Data);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _accountServices.LoginAsync(request);
            if (!result.Success)
            {
                return this.ToErrorResult(result);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _activityLogService.LogAsync(
                userId: result.Data!.UserId,
                action: "Login",
                details: $"User {result.Data.Email} logged in",
                ipAddress: ip);

            return Ok(result.Data);
        }

        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return this.ErrorResult(
                    ErrorCodes.Unauthorized,
                    "Invalid token.",
                    StatusCodes.Status401Unauthorized);
            }

            var token = GetBearerToken(Request);
            if (string.IsNullOrWhiteSpace(token))
            {
                return this.ErrorResult(
                    ErrorCodes.Unauthorized,
                    "Bearer token is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var logoutResult = await _accountServices.LogoutAsync(token, userId);
            if (!logoutResult.Success)
            {
                return this.ToErrorResult(logoutResult);
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _activityLogService.LogAsync(
                userId: userId,
                action: "Logout",
                details: string.IsNullOrWhiteSpace(email)
                    ? "User logged out"
                    : $"User {email} logged out",
                ipAddress: ip);

            return Ok(new { message = logoutResult.Data });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _accountServices.VerifyEmailAsync(token);
            if (!result.Success)
            {
                return this.ToErrorResult(result);
            }

            return Ok(new { message = result.Data });
        }

        // [MỚI] Đây là API Test việc Protect tài nguyên bằng JWT
        [HttpGet("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize] // Bắt buộc phải có Token hợp lệ để chạy được
        public IActionResult GetProfile()
        {
            // Trong API này bạn có quyền đọc các Claims đã được giải mã mà hệ thống lấy được từ Token
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            var fullName = User.FindFirstValue(System.Security.Claims.ClaimTypes.Name);
            var actorType = User.FindFirstValue("actor_type");
            var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Ok(new 
            {

                UserId = userId,
                Email = email,
                FullName = fullName,
                ActorType = actorType,
                Roles = roles
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

        private static string? GetBearerToken(HttpRequest request)
        {
            const string bearerPrefix = "Bearer ";
            var authorizationHeader = request.Headers["Authorization"].ToString();

            if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return authorizationHeader[bearerPrefix.Length..].Trim();
        }
    }
}
