using Microsoft.AspNetCore.Http;

namespace SWP391.Models;

public class ServiceResult<T>
{
    private ServiceResult(bool success, T? data, string? error, string? errorCode, int? statusCode)
    {
        Success = success;
        Data = data;
        Error = error;
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public bool Success { get; }

    public T? Data { get; }

    public string? Error { get; }

    public string? ErrorCode { get; }

    public int? StatusCode { get; }

    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T>(true, data, null, null, null);
    }

    public static ServiceResult<T> Fail(
        string error,
        string errorCode = Common.ErrorCodes.ValidationError,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        return new ServiceResult<T>(false, default, error, errorCode, statusCode);
    }
}
