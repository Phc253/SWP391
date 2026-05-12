namespace SWP391.Middlewares;
using System.Net;
using System.Text.Json;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger; // Để ghi log ra console/file
        _env = env;       // Để biết đang ở môi trường Dev hay Product
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Cho request đi tiếp
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message); // Ghi lại lỗi để Dev xem
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Đây là cái "Hợp đồng" với Frontend (Dùng DTO để thống nhất)
        object response = _env.IsDevelopment()
            ? new { StatusCode = context.Response.StatusCode, Message = ex.Message, Detail = ex.StackTrace?.ToString() }
            : new { StatusCode = context.Response.StatusCode, Message = "Internal Server Error từ hệ thống SWP." };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }
}
