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

        //[HttpGet("throw-error")]
        //public IActionResult ThrowError()
        //{
        //    // Cố tình tạo ra một Exception để test ExceptionMiddleware
        //    throw new Exception("Đây là một lỗi cố tình ném ra để kiểm tra Middleware!");
        //}
    }
}
