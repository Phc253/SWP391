using Microsoft.AspNetCore.Mvc;
using SWP391.Models.Account;
using SWP391.Service;

namespace SWP391.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AccountServices _accountServices;

        public AccountController(AccountServices accountServices)
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
    }
}
