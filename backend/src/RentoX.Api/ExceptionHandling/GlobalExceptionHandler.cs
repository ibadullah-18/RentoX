using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Api.ExceptionHandling;

public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnhandledException(logger, exception);

        int statusCode = exception switch
        {
            DomainException =>
                StatusCodes.Status400BadRequest,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = statusCode ==
                    StatusCodes.Status400BadRequest
                ? "Business rule violation"
                : "An unexpected error occurred",
            Detail = statusCode ==
                     StatusCodes.Status400BadRequest
                ? exception.Message
                : "Please try again later."
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred")]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception);
}