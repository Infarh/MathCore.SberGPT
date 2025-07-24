using Microsoft.Extensions.DependencyInjection;

namespace MathCore.SberGPT.ConsoleTest.Infrastructure.Extensions;

internal static class ServiceProviderEx
{
    public static T Get<T>(this IServiceProvider services) where T : notnull => services.GetRequiredService<T>();

    public static T Get<T>(this IServiceProvider services, string key) where T : notnull => services.GetRequiredKeyedService<T>(key);

    public static T Get<T>(this IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
}
