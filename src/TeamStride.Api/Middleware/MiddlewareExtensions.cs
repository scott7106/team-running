using Microsoft.AspNetCore.Builder;

namespace TeamStride.Api.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static IApplicationBuilder UseTeamResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TeamMiddleware>();
    }

    public static IApplicationBuilder UseApiTeamContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiTeamContextMiddleware>();
    }
} 