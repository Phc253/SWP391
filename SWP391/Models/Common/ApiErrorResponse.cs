namespace SWP391.Models.Common;

public class ApiErrorResponse
{
    public bool Success { get; init; } = false;

    public string Code { get; init; } = ErrorCodes.InternalError;

    public string Message { get; init; } = string.Empty;

    public string? TraceId { get; init; }
}
