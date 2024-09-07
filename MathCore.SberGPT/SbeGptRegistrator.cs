using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public static class SbeGptRegistrator
{
    private const string __BaseUrl = "https://gigachat.devices.sberbank.ru/";

    public static IServiceCollection AddSberGPT(this IServiceCollection services)
    {
        services.AddHttpClient<GptClient>(http => http.BaseAddress = new(__BaseUrl))
            .AddHttpMessageHandler(GetHttpMessageHandler);

        return services;
    }

    private static DelegatingHandler GetHttpMessageHandler(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var log = services.GetRequiredService<ILogger<GptClient>>();
        // ReSharper disable once SettingNotFoundInConfiguration
        return new SberGPTHandler(configuration.GetSection("sber"), log);
    }

    private class SberGPTHandler(IConfiguration config, ILogger log) : DelegatingHandler
    {
        private readonly string _RqUID = Guid.NewGuid().ToString();

        private readonly record struct AccessToken(string Token, DateTimeOffset ExpiredTime)
        {
            public bool Expired => (ExpiredTime - DateTimeOffset.Now).TotalMilliseconds < 500;

            public static implicit operator AuthenticationHeaderValue(AccessToken token) => new("Bearer", token.Token);
        }

        private AccessToken? _AccessToken;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            if (_AccessToken is not { Expired: true } access_token)
                _AccessToken = access_token = await GetAccessToken(cancel).ConfigureAwait(false);

            request.Headers.Authorization = access_token;
            request.Headers.Add("RqUID", _RqUID);

            var response_message = await base.SendAsync(request, cancel);

            return response_message;
        }

        private const string __AuthUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
        private const string __Scope = "GIGACHAT_API_PERS";

        private async Task<AccessToken> GetAccessToken(CancellationToken Cancel)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, __AuthUrl)
            {
                Headers =
                {
                    // ReSharper disable once SettingNotFoundInConfiguration
                    { "Authorization", $"Bearer {config["secret"] ?? throw new InvalidOperationException("Не задан секрет авторизации sber:secret")}" },
                    { "RqUID", _RqUID },
                },
                // ReSharper disable once SettingNotFoundInConfiguration
                Content = new FormUrlEncodedContent([new("scope", config["scope"] ?? __Scope)]),
            };

            var response = await base.SendAsync(request, Cancel).ConfigureAwait(false);

            try
            {
                AccessToken token = await response.EnsureSuccessStatusCode()
                    .Content
                    .ReadFromJsonAsync<GetAccessTokenResponse>(cancellationToken: Cancel)
                    .ConfigureAwait(false);

                log.LogInformation("Успешная авторизация. Токен действует до {ExpiredTime:dd.MM.yyyy HH:mm:ss.fff}",
                    token.ExpiredTime);

                return token;
            }
            catch(HttpRequestException error) when(error.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException("Ошибка формата запроса авторизации", error);
            }
            catch(HttpRequestException error) when(error.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Ошибка данных авторизации", error);
            }
        }

        private readonly record struct GetAccessTokenResponse(
            [property: JsonPropertyName("access_token")]
            string AccessToken,
            [property: JsonPropertyName("expires_at")]
            long ExpiresAt)
        {
            public static implicit operator AccessToken(GetAccessTokenResponse response) => 
                new(response.AccessToken, TimeFromUnixTime(response.ExpiresAt));

            private static DateTime TimeFromUnixTime(long UnixSecondsFrom1970) => DateTime.UnixEpoch.AddSeconds(UnixSecondsFrom1970 / 1000d);
        }
    }
}