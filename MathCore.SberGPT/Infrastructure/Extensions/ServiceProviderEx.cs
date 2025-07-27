using Microsoft.Extensions.DependencyInjection;

namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class ServiceProviderEx
{
    public static T Get<T>(this IServiceProvider services) where T : notnull => services.GetRequiredService<T>();

    public static T Get<T>(this IServiceProvider services, string key) where T : notnull => services.GetRequiredKeyedService<T>(key);
}
