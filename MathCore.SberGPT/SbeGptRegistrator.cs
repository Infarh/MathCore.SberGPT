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
        // Убираем прямую регистрацию SberGPTRequestHandler
        services
            .AddHttpClient<GptClient>(http => http.BaseAddress = new(GptClient.BaseUrl))
            .AddHttpMessageHandler(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<GptClient>>();
                return new SberGPTRequestHandler(configuration.GetSection("sber"), logger);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(30));

        return services;
    }
}