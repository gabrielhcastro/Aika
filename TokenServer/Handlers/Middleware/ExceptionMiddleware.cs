using System.Net;
using System.Text.Json;

namespace TokenServer.Handlers.Middleware;

public class ExceptionMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        }
        catch(Exception ex) {
            _logger.LogError(ex, "An unhandled exception occurred.");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new {
                context.Response.StatusCode,
                Message = "An unexpected error occurred.",
                DetailedMessage = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

public static class ExceptionMiddlewareExtensions {
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder) {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}
