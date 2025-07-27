using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

/// <summary>Регистрация клиента SberGPT в DI-контейнере</summary>
public static class SbeGptRegistrator
{
    /// <summary>Добавляет клиента SberGPT в сервисы</summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов</returns>
    public static IServiceCollection AddSberGPT(this IServiceCollection services)
    {
        services.AddHttpClient<GptClient>(http => http.BaseAddress = new(GptClient.BaseUrl))
            .AddHttpMessageHandler(GetHttpMessageHandler)
            .SetHandlerLifetime(TimeSpan.FromMinutes(30));

        return services;
    }

    /// <summary>Создаёт обработчик HTTP-запросов для SberGPT</summary>
    /// <param name="services">Провайдер сервисов</param>
    /// <returns>Делегирующий обработчик</returns>
    [SuppressMessage("ReSharper", "SettingNotFoundInConfiguration")]
    private static DelegatingHandler GetHttpMessageHandler(IServiceProvider services)
    {
        var configuration = services.Get<IConfiguration>();
        var log = services.Get<ILogger<GptClient>>();
        return new SberGPTRequestHandler(configuration.GetSection("sber"), log);
    }
}