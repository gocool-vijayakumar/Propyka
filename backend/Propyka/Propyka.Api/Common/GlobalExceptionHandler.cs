using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Propyka.Api.Common;

/// <summary>
/// Catches anything that escapes a controller and turns it into a
/// ProblemDetails response, so the client never sees a stack trace.
///
/// DomainException and NotFoundException carry messages meant for users, so
/// their text is passed through. Everything else is logged in full and
/// answered with a generic 500 — an unexpected exception's message can easily
/// contain a connection string or a file path.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found.",
                Detail = notFound.Message
            },

            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "That request could not be completed.",
                Detail = domain.Message
            },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }
        };

        problem.Instance = context.Request.Path;

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);
        }
        else
        {
            // Expected outcomes — worth a trace, not an error page in the logs.
            _logger.LogInformation(
                "Handled {ExceptionType} on {Path}: {Message}",
                exception.GetType().Name, context.Request.Path, exception.Message);
        }

        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true; // handled — stop the pipeline
    }
}
