using ManagementSystem.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Management_System.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            RequestValidationException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            EmailAlreadyRegisteredException => (StatusCodes.Status409Conflict, "Email already registered", exception.Message),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Authentication failed", exception.Message),
            ForbiddenOperationException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "The request could not be completed.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled API exception. Trace ID: {TraceId}", httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogInformation(exception, "API request returned {StatusCode}", status);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Extensions = { ["traceId"] = httpContext.TraceIdentifier }
            },
            cancellationToken);

        return true;
    }
}
