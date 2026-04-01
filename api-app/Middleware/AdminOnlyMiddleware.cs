using api_app.Repositories.Interfaces;

namespace api_app.Middleware;

public class AdminOnlyMiddleware
{
    private readonly RequestDelegate _next;

    public AdminOnlyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (!context.Request.Path.StartsWithSegments("/api/admin"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-User-Id", out var value))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing X-User-Id header.");
            return;
        }

        if (!int.TryParse(value.ToString(), out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid X-User-Id header.");
            return;
        }

        var user = await userRepository.GetByIdAsync(userId, context.RequestAborted);
        if (user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("User not found.");
            return;
        }

        if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Admin access required.");
            return;
        }

        await _next(context);
    }
}
