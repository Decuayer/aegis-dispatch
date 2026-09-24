using System.Net;
using System.Text.Json;
using SocarDispatch.Application.Common.Exceptions;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => CreateResponse(
                context,
                HttpStatusCode.BadRequest,
                "A validation error occurred.",
                validationEx.Errors.SelectMany(kvp => kvp.Value).ToList()),

            EntityNotFoundException notFoundEx => CreateResponse(
                context,
                HttpStatusCode.NotFound,
                notFoundEx.Message),

            KeyNotFoundException keyNotFoundEx => CreateResponse(
                context,
                HttpStatusCode.NotFound,
                keyNotFoundEx.Message),

            ForbiddenAccessException forbiddenEx => CreateResponse(
                context,
                HttpStatusCode.Forbidden,
                forbiddenEx.Message),

            DomainException domainEx => CreateResponse(
                context,
                HttpStatusCode.BadRequest,
                domainEx.Message),

            _ => CreateResponse(
                context,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred on the server side.")
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    private static ApiResponse<object> CreateResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        List<string>? errors = null)
    {
        context.Response.StatusCode = (int)statusCode;
        return ApiResponse<object>.FailureResult(message, errors);
    }
}
