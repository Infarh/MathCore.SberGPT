using Microsoft.Extensions.DependencyInjection;

namespace MathCore.SberGPT.Infrastructure.Extensions;

/// <summary>Статический класс-расширение для IServiceProvider.</summary>
internal static class ServiceProviderEx
{
    /// <summary>Получает обязательный сервис типа T.</summary>
    public static T Get<T>(this IServiceProvider services) where T : notnull => services.GetRequiredService<T>();

    /// <summary>Получает обязательный сервис типа T по ключу.</summary>
    public static T Get<T>(this IServiceProvider services, string key) where T : notnull => services.GetRequiredKeyedService<T>(key);
}
