using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Account;
using SWP391.Service;

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
    }
}
