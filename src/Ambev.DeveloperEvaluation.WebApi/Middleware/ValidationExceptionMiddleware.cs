using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationExceptionMiddleware> _logger;

    public ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
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
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (DomainException ex)
        {
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            await HandleInvalidOperationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleUnexpectedExceptionAsync(context);
        }
    }

    private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var response = new ApiResponse
        {
            Success = false,
            Message = "Validation Failed",
            Errors = exception.Errors.Select(error => (ValidationErrorDetail)error)
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task HandleDomainExceptionAsync(HttpContext context, DomainException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        var response = new ApiResponse
        {
            Success = false,
            Message = exception.Message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        var response = new ApiResponse
        {
            Success = false,
            Message = exception.Message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var response = new ApiResponse
        {
            Success = false,
            Message = exception.Message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task HandleInvalidOperationExceptionAsync(HttpContext context, InvalidOperationException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status409Conflict;

        var response = new ApiResponse
        {
            Success = false,
            Message = exception.Message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task HandleUnexpectedExceptionAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new ApiResponse
        {
            Success = false,
            Message = "An unexpected error occurred. Please try again later."
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
