using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
// ReSharper disable SettingNotFoundInConfiguration

namespace MathCore.SberGPT;

/// <summary>Обработчик HTTP-запросов для клиента SberGPT с поддержкой авторизации и трассировки</summary>
public class SberGPTRequestHandler : DelegatingHandler
{
    /// <summary>Уникальный идентификатор запросов от данного экземпляра клиента для трассировки запросов</summary>
    private readonly string _RqUID = Guid.NewGuid().ToString();

    /// <summary>Токен доступа</summary>
    private AccessToken? _AccessToken;

    /// <summary>Конфигурация клиента</summary>
    private readonly IConfiguration _Config;
    /// <summary>Логгер для вывода диагностической информации</summary>
    private readonly ILogger _Log;
    /// <summary>Идентификатор клиента</summary>
    private readonly string _ClientId;
    /// <summary>Значение заголовка User-Agent</summary>
    private readonly ProductInfoHeaderValue _UserAgent;

    /// <summary>Инициализация нового экземпляра <see cref="SberGPTRequestHandler"/></summary>
    /// <param name="Config">Конфигурация клиента</param>
    /// <param name="Log">Логгер</param>
    public SberGPTRequestHandler(IConfiguration Config, ILogger Log)
    {
        _Config = Config;
        _Log = Log;

        _ClientId = _Config["ClientId"] ?? Guid.NewGuid().ToString();
        _UserAgent = ProductInfoHeaderValue.Parse(_Config["UserAgent"] ??= "MathCore.SberGPT/1.0");

        //InnerHandler ??= new HttpClientHandler
        //{
        //    ClientCertificateOptions = ClientCertificateOption.Manual,
        //    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        //    AutomaticDecompression = DecompressionMethods.All
        //};
    }

    /// <summary>Опции сериализации JSON для отладки</summary>
    private static readonly JsonSerializerOptions __RequestSerializationOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
    };

    /// <summary>Выполняет отправку HTTP-запроса с автоматическим управлением токеном доступа</summary>
    /// <param name="request">HTTP-запрос</param>
    /// <param name="cancel">Токен отмены</param>
    /// <returns>HTTP-ответ</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
    {
        if (_AccessToken is not { Expired: false } access_token)
        {
            if (_Log.IsEnabled(LogLevel.Trace))
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (_AccessToken is null)
                    _Log.LogTrace("Токен доступа отсутствует. Выполняю запрос токена.");
                else
                    _Log.LogTrace("Токен доступа просрочен. Выполняю запрос нового.");

            _AccessToken = access_token = await GetAccessToken(cancel).ConfigureAwait(false);
        }

        request.Headers.Authorization = access_token;
        request.Headers.Add(GptClient.RequesIdHeader, _RqUID);
        request.Headers.Add(GptClient.ClientXIdHeader, _ClientId);

        if (request.Headers.UserAgent.Count == 0)
            request.Headers.UserAgent.Add(_UserAgent);

        // Логируем исходящий HTTP-запрос для отладки (Debug)
        //if (_Log.IsEnabled(LogLevel.Debug))
        //{
        //    var content_str = request.Content is not null
        //        ? await request.Content.ReadAsStringAsync(cancel)
        //        : null;

        //    var request_info = new
        //    {
        //        Method = request.Method.ToString(), // HTTP-метод
        //        Uri = request.RequestUri?.ToString(), // URI запроса
        //        Headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)), // Заголовки
        //        Content = content_str // Тело запроса
        //    };

        //    var request_text = JsonSerializer.Serialize(request_info, __RequestSerializationOptions);
        //    _Log.LogDebug("Исходящий запрос: {Request}", request_text);
        //}

        var response_message = await base.SendAsync(request, cancel);

        return response_message;
    }

    /// <summary>Стандартная область авторизации</summary>
    private const string __DefaultScope = "GIGACHAT_API_PERS";

    /// <summary>Выполняет получение токена доступа с возможностью кэширования и шифрования</summary>
    /// <param name="Cancel">Токен отмены</param>
    /// <returns>Токен доступа</returns>
    [SuppressMessage("ReSharper", "SettingNotFoundInConfiguration")]
    private async Task<AccessToken> GetAccessToken(CancellationToken Cancel)
    {
        FileInfo? token_store_file = null;

        var token_store_key = _Config["TokenEncryptionKey"];
        var token_store_file_name = _Config["TokenStoreFile"] ?? "chat-gpt.token";

        if (token_store_key is { Length: > 0 })
        {
            token_store_file = new(token_store_file_name);
            if (token_store_file.Exists)
            {
                var pass_bytes = token_store_key.GetSHA512();

                var key_bytes = pass_bytes[..32];
                var iv_bytes = pass_bytes[^16..];

                var aes = Aes.Create();
                aes.Key = key_bytes;
                aes.IV = iv_bytes;

                try
                {
                    using var decryptor = aes.CreateDecryptor();
                    await using var crypto_stream = new CryptoStream(token_store_file.OpenRead(), decryptor, CryptoStreamMode.Read);

                    var token = await JsonSerializer.DeserializeAsync<AccessToken>(
                            crypto_stream,
                            GptClientJsonSerializationContext.Default.Options,
                            Cancel)
                            .ConfigureAwait(false);

                    if (token is { Expired: false })
                    {
                        _Log.LogInformation(@"Токен загружен из файла. Действует до {ExpiredTime:dd.MM.yyyy HH:mm:ss.fff} (осталось времени {DurationTime:hh\:mm\:ss\.fff})",
                            token.ExpiredTime,
                            token.DurationTime);
                        return token;
                    }

                    if (token.ExpiredTime != default)
                        _Log.LogInformation("Токен загружен из файла. Действует было завершено {ExpiredTime:dd.MM.yyyy HH:mm:ss.fff}", token.ExpiredTime);
                    else
                        _Log.LogWarning("Ошибка загрузки токена из файла.");
                }
                catch (Exception)
                {
                    token_store_file.Delete();
                }
            }
        }

        var secret = _Config["secret"].NotNull("Не задан секрет авторизации sber:secret");
        var scope = _Config["scope"] ?? __DefaultScope;

        _Log.LogInformation("Запрос авторизации c областью {Scope}", scope);

        var request = new HttpRequestMessage(HttpMethod.Post, GptClient.AuthUrl)
        {
            Headers =
            {
                { "Authorization", $"Bearer {secret}" },
                { "User-Agent", _Config["userAgent"] },
                { GptClient.RequesIdHeader, _RqUID },
                { GptClient.ClientXIdHeader, _ClientId },
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

            _Log.LogInformation(@"Успешная авторизация. Токен действует до {ExpiredTime:dd.MM.yyyy HH:mm:ss.fff} (осталось времени {DurationTime:hh\:mm\:ss\.fff})",
                token.ExpiredTime,
                token.DurationTime);

            if (token_store_file is null)
                return token;

            var pass_bytes = token_store_key!.GetSHA512();

            var key_bytes = pass_bytes[..32];
            var iv_bytes = pass_bytes[^16..];

            var aes = Aes.Create();
            aes.Key = key_bytes;
            aes.IV = iv_bytes;

            using var encryptor = aes.CreateEncryptor();
            await using var crypto_stream = new CryptoStream(token_store_file.Create(), encryptor, CryptoStreamMode.Write);

            await JsonSerializer.SerializeAsync(
                    crypto_stream,
                    token,
                    GptClientJsonSerializationContext.Default.Options,
                    Cancel)
                .ConfigureAwait(false);

            _Log.LogInformation("Токен был сохранён в файл {TokenStoreFilePath}.", token_store_file.FullName);

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

    /// <summary>Структура ответа на запрос токена доступа</summary>
    /// <param name="AccessToken">Строка токена</param>
    /// <param name="ExpiresAt">Время истечения срока действия (Unix-время)</param>
    private readonly record struct GetAccessTokenResponse(
        [property: JsonPropertyName("access_token")]
        string AccessToken,
        [property: JsonPropertyName("expires_at")]
        long ExpiresAt)
    {
        /// <summary>Неявное преобразование к <see cref="AccessToken"/></summary>
        /// <param name="response">Ответ сервера</param>
        public static implicit operator AccessToken(GetAccessTokenResponse response) =>
            new(response.AccessToken, TimeFromUnixTime(response.ExpiresAt));

        /// <summary>Преобразование Unix-времени в <see cref="DateTime"/></summary>
        /// <param name="UnixSecondsFrom1970">Unix-время в миллисекундах</param>
        /// <returns>Дата и время</returns>
        private static DateTime TimeFromUnixTime(long UnixSecondsFrom1970) => DateTime.UnixEpoch.AddSeconds(UnixSecondsFrom1970 / 1000d);
    }
}