using Microsoft.AspNetCore.Mvc;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is okay");
        }
    }
}
