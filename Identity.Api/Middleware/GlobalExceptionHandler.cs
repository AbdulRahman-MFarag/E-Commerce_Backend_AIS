using Identity.Api.Common.Exceptions;
using System;
using System.Threading;
using Microsoft.AspNetCore.Diagnostics;

namespace Identity.Api.Middleware;

public class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception occurred.");

        var statusCode = exception switch
        {
            ConflictException => StatusCodes.Status409Conflict,

            UnauthorizedException =>
                StatusCodes.Status401Unauthorized,

            NotFoundException =>
                StatusCodes.Status404NotFound,

            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode =
            statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                statusCode,
                message = exception.Message,
                traceId = httpContext.TraceIdentifier
            },
            cancellationToken);

        return true;
    }
}