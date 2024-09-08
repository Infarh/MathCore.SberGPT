using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public static class SbeGptRegistrator
{
    private const string __BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1/";

    public static IServiceCollection AddSberGPT(this IServiceCollection services)
    {
        services.AddHttpClient<GptClient>(http => http.BaseAddress = new(__BaseUrl))
            .AddHttpMessageHandler(GetHttpMessageHandler)
            .SetHandlerLifetime(TimeSpan.FromMinutes(30))
            ;

        return services;
    }

    private static DelegatingHandler GetHttpMessageHandler(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var log = services.GetRequiredService<ILogger<GptClient>>();
        // ReSharper disable once SettingNotFoundInConfiguration
        return new SberGPTRequestHandler(configuration.GetSection("sber"), log);
    }
}