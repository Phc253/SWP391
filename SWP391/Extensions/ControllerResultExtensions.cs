using Microsoft.AspNetCore.Mvc;
using SWP391.Models;
using SWP391.Models.Common;

namespace SWP391.Extensions;

public static class ControllerResultExtensions
{
    public static IActionResult ToErrorResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        var statusCode = result.StatusCode ?? StatusCodes.Status400BadRequest;
        var response = new ApiErrorResponse
        {
            Code = result.ErrorCode ?? ErrorCodes.ValidationError,
            Message = result.Error ?? "Request failed.",
            TraceId = controller.HttpContext.TraceIdentifier
        };
        return controller.StatusCode(statusCode, response);
    }

    public static IActionResult ErrorResult(
        this ControllerBase controller,
        string code,
        string message,
        int statusCode)
    {
        return controller.StatusCode(statusCode, new ApiErrorResponse
        {
            Code = code,
            Message = message,
            TraceId = controller.HttpContext.TraceIdentifier
        });
    }
}
