using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;

namespace RaceDay.Api.Services;

// Outcome of a service operation.
public class ServiceResult<T>
{
    public int StatusCode { get; private init; }
    public T? Value { get; private init; }
    public string? ErrorMessage { get; private init; }

    public bool IsSuccess => StatusCode is >= 200 and < 300;

    public static ServiceResult<T> Ok(T value) => new() { StatusCode = StatusCodes.Status200OK, Value = value };
    public static ServiceResult<T> Created(T value) => new() { StatusCode = StatusCodes.Status201Created, Value = value };
    public static ServiceResult<T> NoContent() => new() { StatusCode = StatusCodes.Status204NoContent };
    public static ServiceResult<T> BadRequest(string message) => new() { StatusCode = StatusCodes.Status400BadRequest, ErrorMessage = message };
    public static ServiceResult<T> Unauthorized(string message) => new() { StatusCode = StatusCodes.Status401Unauthorized, ErrorMessage = message };
    public static ServiceResult<T> Forbidden(string message) => new() { StatusCode = StatusCodes.Status403Forbidden, ErrorMessage = message };
    public static ServiceResult<T> NotFound(string message) => new() { StatusCode = StatusCodes.Status404NotFound, ErrorMessage = message };
    public static ServiceResult<T> Conflict(string message) => new() { StatusCode = StatusCodes.Status409Conflict, ErrorMessage = message };

    // Converts the result to the matching HTTP response.
    public IActionResult ToActionResult(ControllerBase controller, string? createdAtRoute = null)
    {
        if (!IsSuccess)
        {
            return controller.StatusCode(StatusCode, new ApiError { Message = ErrorMessage ?? string.Empty });
        }

        return StatusCode switch
        {
            StatusCodes.Status201Created => controller.StatusCode(StatusCodes.Status201Created, Value),
            StatusCodes.Status204NoContent => controller.NoContent(),
            _ => controller.Ok(Value)
        };
    }
}
