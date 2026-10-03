using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SecureHR.Application.Exceptions;

namespace SecureHR.Api.Middleware
{
    /// <summary>
    /// Turns unhandled exceptions into RFC 7807 ProblemDetails responses.
    /// Known application exceptions map to 4xx with their (application-written) message;
    /// anything else is logged and returned as a generic 500 with no internal details.
    /// </summary>
    public class GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
        {
            var (status, title, detail) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", null),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", (string?)null)
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            else
                logger.LogInformation("Request failed with {Status}: {Message}", status, exception.Message);

            httpContext.Response.StatusCode = status;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail
                }
            });
        }
    }
}
