using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public class SberGPTRequestHandler : DelegatingHandler
{
    /// <summary>Уникальный идентификатор запросов от данного экземпляра клиента для трассировки запросов</summary>
    private readonly string _RqUID = Guid.NewGuid().ToString();

    /// <summary>Токен доступа</summary>
    private AccessToken? _AccessToken;

    private readonly IConfiguration _Config;
    private readonly ILogger _Log;

    public SberGPTRequestHandler(IConfiguration Config, ILogger Log)
    {
        _Config = Config;
        _Log = Log;

        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
    {
        if (_AccessToken is not { Expired: true } access_token)
            _AccessToken = access_token = await GetAccessToken(cancel).ConfigureAwait(false);

        request.Headers.Authorization = access_token;
        request.Headers.Add(GptClient.RequesIdHeader, _RqUID);

        var response_message = await base.SendAsync(request, cancel);

        return response_message;
    }

    private const string __DefaultScope = "GIGACHAT_API_PERS";

    [SuppressMessage("ReSharper", "SettingNotFoundInConfiguration")]
    private async Task<AccessToken> GetAccessToken(CancellationToken Cancel)
    {
        var secret = _Config["secret"].NotNull("Не задан секрет авторизации sber:secret");
        var scope = _Config["scope"] ?? __DefaultScope;

        _Log.LogInformation("Запрос авторизации c областью {Scope}", scope);

        var request = new HttpRequestMessage(HttpMethod.Post, GptClient.AuthUrl)
        {
            Headers =
            {
                { "Authorization", $"Bearer {secret}" },
                { "RqUID", _RqUID },
            },
            Content = new FormUrlEncodedContent([new("scope", scope)]),
        };

        var response = await base.SendAsync(request, Cancel).ConfigureAwait(false);

        try
        {
            AccessToken token = await response.EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<GetAccessTokenResponse>(cancellationToken: Cancel)
                .ConfigureAwait(false);

            _Log.LogInformation("Успешная авторизация. Токен действует до {ExpiredTime:dd.MM.yyyy HH:mm:ss.fff}",
                token.ExpiredTime);

            return token;
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException("Ошибка формата запроса авторизации", error);
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            var response_error_info = await response
                .Content
                .ReadFromJsonAsync<ErrorResponseInfo>(cancellationToken: Cancel)
                .ConfigureAwait(false);

            _Log.LogError("Ошибка авторизации: {Error}", response_error_info?.Message ?? "Unauthorized");

            throw new InvalidOperationException($"Ошибка данных авторизации: {response_error_info}", error);
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