using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.RfcExceptionsHandler.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    public static IServiceCollection AddDefaultRfcExceptionHandleMiddleware(this IServiceCollection services)
    {
        return services
            .AddSingleton<IRfcExceptionHandleConfig, DefaultRfcExceptionHandleConfig>()
            .AddSingleton<RfcExceptionHandleMiddleware>();
    }

    public static IServiceCollection AddRfcExceptionHandleMiddleware<T>(this IServiceCollection services) where T : class, IRfcExceptionHandleConfig
    {
        return services
            .AddSingleton<IRfcExceptionHandleConfig, T>()
            .AddSingleton<RfcExceptionHandleMiddleware>();
    }

    public static IApplicationBuilder UseRfcExceptionHandleMiddleware(this IApplicationBuilder app) => app.UseMiddleware<RfcExceptionHandleMiddleware>();
}