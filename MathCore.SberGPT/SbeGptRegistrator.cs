using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public static class SbeGptRegistrator
{

    public static IServiceCollection AddSberGPT(this IServiceCollection services)
    {
        services.AddHttpClient<GptClient>(http => http.BaseAddress = new(GptClient.BaseUrl))
            .AddHttpMessageHandler(GetHttpMessageHandler)
            .SetHandlerLifetime(TimeSpan.FromMinutes(30))
            ;

        return services;
    }

    [SuppressMessage("ReSharper", "SettingNotFoundInConfiguration")]
    private static DelegatingHandler GetHttpMessageHandler(IServiceProvider services)
    {
        var configuration = services.Get<IConfiguration>();
        var log = services.Get<ILogger<GptClient>>();
        return new SberGPTRequestHandler(configuration.GetSection("sber"), log);
    }
}