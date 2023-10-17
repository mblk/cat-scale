using System.Net;
using System.Text.Json;
using CatScale.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // TODO ValidationProblemDetails vs ProblemDetails ?
        ProblemDetails problemDetails;

        switch (ex)
        {
            case EntityNotFoundException entityNotFoundException:
                problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Instance = context.Request.Path,
                    Detail = entityNotFoundException.Message,
                };
                break;

            case DomainValidationException domainValidationException:
                problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request",
                    Status = (int)HttpStatusCode.BadRequest,
                    Instance = context.Request.Path,
                    Detail = domainValidationException.Message,
                };
                break;

            default:
                _logger.LogError(ex, "An unhandled exception has occurred: {Message}", ex.Message);
                problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Internal Server Error",
                    Status = (int)HttpStatusCode.InternalServerError,
                    Instance = context.Request.Path,
                    Detail = ex.Message,
                };
                break;
        }

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}