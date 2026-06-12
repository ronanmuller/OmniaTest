using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

public class UnauthorizedResponseMiddleware
{
    private readonly RequestDelegate _next;

    public UnauthorizedResponseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized
            && !context.Response.HasStarted)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse
            {
                Success = false,
                Message = "Authentication required. Provide a valid Bearer token in the Authorization header."
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }
    }
}
