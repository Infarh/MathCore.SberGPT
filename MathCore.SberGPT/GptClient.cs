using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using MathCore.SberGPT.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace MathCore.SberGPT;

/// <summary>Клиент для запросов к Giga chat</summary>
/// <remarks>Клиент для запросов к Giga chat</remarks>
/// <param name="Http">Http-клиент для отправки запросов</param>
/// <param name="Log">Логгер</param>
public partial class GptClient(HttpClient Http, ILogger<GptClient>? Log)
{
    //internal const string BaseUrl = "http://localhost:8881";
    internal const string BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1/";
    internal const string AuthUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
    internal const string RequesIdHeader = "RqUID";
    internal const string ClientXIdHeader = "X-Client-ID";
    internal const string RequestXIdHeader = "X-Request-ID";
    internal const string SessionXIdHeader = "X-Session-ID";

    private readonly ILogger _Log = Log ?? NullLogger<GptClient>.Instance;

    #region Конструктор

    /// <summary>Клиент для запросов к Giga chat</summary>
    public GptClient(IConfiguration config, ILogger<GptClient>? Log = null)
        : this(
            Http: new(new SberGPTRequestHandler(config, Log)) { BaseAddress = new(BaseUrl) },
            Log: Log)
    {

    }

    /// <summary>Создаёт конфигурацию для клиента GigaChat на основе секрета и области</summary>
    /// <param name="Secret">Секретный ключ для авторизации</param>
    /// <param name="Scope">Область доступа (по умолчанию "GIGACHAT_API_PERS")</param>
    /// <returns>Объект конфигурации</returns>
    private static IConfiguration GetConfig(string Secret, string? Scope) => new ConfigurationBuilder()
        .AddInMemoryCollection([new("secret", Secret), new("scope", Scope ?? "GIGACHAT_API_PERS")]).Build();

    /// <summary>Инициализирует новый экземпляр клиента GigaChat с использованием секрета и области</summary>
    /// <param name="Secret">Секретный ключ для авторизации</param>
    /// <param name="Scope">Область доступа (по умолчанию "GIGACHAT_API_PERS")</param>
    /// <param name="Log">Логгер для вывода диагностической информации</param>
    public GptClient(string Secret, string? Scope = null, ILogger<GptClient>? Log = null)
        : this(
            Http: new(new SberGPTRequestHandler(GetConfig(Secret, Scope), Log ?? NullLogger<GptClient>.Instance)) { BaseAddress = new(BaseUrl) },
            Log: Log ?? NullLogger<GptClient>.Instance)
    {

    }

    #endregion

    /// <summary>Системный запрос</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>История запросов и ответов в текущей сессии чата</summary>
    private readonly List<Request> _ChatHistory = [];

    /// <summary>Получить список всех запросов в истории чата в режиме только для чтения</summary>
    public IReadOnlyList<Request> Requests => _ChatHistory.AsReadOnly();

    /// <summary>Определяет, использовать ли историю чата при отправке запросов к модели</summary>
    public bool UseChatHistory { get; set; } = true;

    /// <summary>Содержимое заголовка X-Session-Id</summary>
    public string? SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Содержимое заголовка X-Request-Id</summary>
    public string? RequestId { get; set; }

    #region Информация о типах моделей

    /// <summary>Получить список моделей</summary>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Список моделей сервиса</returns>
    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken Cancel = default)
    {
        const string url = "models";

        var response = await Http
            .GetFromJsonAsync<ModelsInfosListResponse>(url, Cancel)
            .ConfigureAwait(false);

        var models = response.Models;
        var result = new string[models.Length];
        for (var i = 0; i < result.Length; i++)
            result[i] = models[i].ModelId;

        return result;
    }

    #endregion

    #region Запрос данных о количестве токенов

    /// <summary>Получить количество токенов для указанной строки ввода</summary>
    /// <param name="Input">Строка ввода</param>
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat-2"</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Информация о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<TokensCount> GetTokensCountAsync(string Input, string Model = "GigaChat-2", CancellationToken Cancel = default)
    {
        var tokens = await GetTokensCountAsync([Input], Model, Cancel).ConfigureAwait(false);
        return tokens[0];
    }

    /// <summary>Получить количество токенов для указанной строки ввода</summary>
    /// <param name="Input">Строки ввода</param>
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat-2"</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Массив с информацией о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<TokensCountInfo> GetTokensCountAsync(IEnumerable<string> Input, string Model = "GigaChat-2", CancellationToken Cancel = default)
    {
        var input = Input.ToArray();
        const string url = "tokens/count";
        var response = await Http
            .PostAsJsonAsync<GetTokensCountRequest>(url, new(Model, input), cancellationToken: Cancel)
            .ConfigureAwait(false);

        var counts = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<TokensCountResponse[]>(cancellationToken: Cancel)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException();

        var result = new TokensCount[counts.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var (tokens, chars) = counts[i];
            result[i] = new(input[i], tokens, chars);
        }

        return result;
    }

    #endregion

    #region Запрос модели

    /// <summary>Запрос модели</summary>
    /// <param name="UserPrompt">Запрос пользователя</param>
    /// <param name="SystemPrompt">Системный запрос</param>
    /// <param name="ChatHistory">История запросов</param>
    /// <param name="ModelName">Название модели</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Результат</returns>
    public async Task<ModelResponse> RequestAsync(
        string UserPrompt,
        string? SystemPrompt = null,
        IEnumerable<Request>? ChatHistory = null,
        string ModelName = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        const string url = "chat/completions";

        switch (UserPrompt)
        {
            case null: throw new ArgumentNullException(nameof(UserPrompt), "Параметр не может быть null");
            case { Length: 0 }: throw new ArgumentException("Параметр не может быть пустым", nameof(UserPrompt));
        }

        List<Request> requests = [];

        requests.AddSystem(SystemPrompt ?? this.SystemPrompt);
        requests.AddHistory(ChatHistory ?? _ChatHistory);
        requests.AddUser(UserPrompt);

        ModelRequest message = new(
            Requests: [.. requests],
            Model: ModelName,
            FunctionSchemes: _Functions.Count > 0
                ? _Functions.Values.Select(f => f.Scheme)
                : null
            );

        var request = GetRequestMessageJson(HttpMethod.Post, url, message, JsonOptions);

        var response = await Http.SendAsync(request, Cancel).ConfigureAwait(false);

        var response_message = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<ModelResponse>(JsonOptions, Cancel)
            .ConfigureAwait(false);

        while (response_message.Choices is [{ FinishReason: "function_call", Message: { FunctionCall: { Name: var func_name, Arguments: var args } function_call, FunctionsStateId: var call_id } }, ..])
        {
            _Log.LogInformation("Модель запросила вызов функции {FunctionName}", func_name);
            var function = _Functions[func_name];
            var func_invoke_result = function.Invoke(args);

            var func_invoke_result_json = JsonSerializer.Serialize(func_invoke_result);

            requests.AddFunctionCall(func_invoke_result_json, call_id, func_name, function_call);

            message = new(
                Requests: [.. requests],
                Model: ModelName,
                FunctionSchemes: _Functions.Values.Select(f => f.Scheme)
            );

            request = GetRequestMessageJson(HttpMethod.Post, url, message, JsonOptions);

            response = await Http.SendAsync(request, Cancel).ConfigureAwait(false);

            response_message = await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<ModelResponse>(JsonOptions, Cancel)
                .ConfigureAwait(false);
        }

        _Log.LogInformation("Успешно получен ответ от модели {ModelType}. Токенов в запросе: {PromptTokens}. Потрачено токенов: {CompletionTokens}. Оплачено токенов {PrecachedPromptTokens} Итого токенов: {TotalTokens}",
            response_message.Model,
            response_message.Usage.PromptTokens,
            response_message.Usage.CompletionTokens,
            response_message.Usage.PrecachedPromptTokens,
            response_message.Usage.TotalTokens);

        _ChatHistory.AddRange(requests);
        _ChatHistory.Add(Request.Assistant(response_message));

        return response_message;
    }

    /// <summary>Асинхронно выполняет потоковый запрос к модели с возможностью получения частичных ответов</summary>
    /// <param name="UserPrompt">Запрос пользователя для отправки модели</param>
    /// <param name="SystemPrompt">Системный промпт для задания роли модели (опционально)</param>
    /// <param name="ChatHistory">История предыдущих сообщений в чате (опционально)</param>
    /// <param name="ModelName">Название используемой модели (по умолчанию "GigaChat-2")</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронный поток сообщений <see cref="StreamingResponseMsg"/> от модели</returns>
    /// <exception cref="ArgumentNullException">Возникает, если <paramref name="UserPrompt"/> равен null</exception>
    /// <exception cref="ArgumentException">Возникает, если <paramref name="UserPrompt"/> является пустой строкой</exception>
    /// <exception cref="HttpRequestException">Возникает при ошибках HTTP-запроса</exception>
    public async IAsyncEnumerable<StreamingResponseMsg> RequestStreamingAsync(
       string UserPrompt,
       string? SystemPrompt = null,
       IEnumerable<Request>? ChatHistory = null,
       string ModelName = "GigaChat-2",
       [EnumeratorCancellation] CancellationToken Cancel = default)
    {
        const string url = "chat/completions";

        switch (UserPrompt)
        {
            case null: throw new ArgumentNullException(nameof(UserPrompt), "Параметр не может быть null");
            case { Length: 0 }: throw new ArgumentException("Параметр не может быть пустым", nameof(UserPrompt));
        }

        List<Request> requests = [];

        requests.AddSystem(SystemPrompt ?? this.SystemPrompt);
        requests.AddHistory(ChatHistory ?? _ChatHistory);
        requests.AddUser(UserPrompt);

        var response_message = new StringBuilder();
        var function_call = true;

        while (function_call)
        {
            function_call = false;

            ModelRequest message = new(
            Requests: [.. requests],
            Model: ModelName,
            FunctionSchemes: _Functions.Count > 0
                ? _Functions.Values.Select(f => f.Scheme)
                : null)
            { Streaming = true };

            var request = GetRequestMessageJson(HttpMethod.Post, url, message, JsonOptions);

            var response = await Http
                .SendAsync(request, Cancel)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var response_content = await response.Content.ReadAsStringAsync(Cancel);
                throw new HttpRequestException($"Bad request: {response_content}");
            }

            await using var stream = await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync(Cancel)
                .ConfigureAwait(false);

            var reader = new StreamReader(stream);

            ResponseChoiceMsgFunc? function_call_value = null;
            Guid? function_call_state_id = null;


            while (await reader.ReadLineAsync(Cancel) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line == "data: [DONE]")
                    break;

                if (!line.StartsWith("data: "))
                    continue;

                var data = line[6..].Replace("\n", Environment.NewLine);
                var msg = JsonSerializer.Deserialize<StreamingResponseMsg>(data, JsonOptions);

                var is_msg = true;
                if (msg.FunctionCall is { } call)
                {
                    function_call_value = call;
                    function_call = true;
                    is_msg = false;
                }

                if (msg.FunctionCallStateId is { } call_id)
                {
                    function_call_state_id = call_id;
                    is_msg = false;
                }

                if (is_msg)
                    yield return msg;

                response_message.AppendLine(msg.MessageAssistant);
            }

            if (!function_call) break;
            var func_name = function_call_value!.Value.Name;
            var args = function_call_value.Value.Arguments;

            _Log.LogInformation("Модель запросила вызов функции {FunctionName}", func_name);
            var function = _Functions[func_name];
            var func_invoke_result = function.Invoke(args);

            var func_invoke_result_json = JsonSerializer.Serialize(func_invoke_result);

            requests.AddFunctionCall(func_invoke_result_json, function_call_state_id!.Value, func_name, function_call_value.Value);
        }

        _ChatHistory.AddUser(UserPrompt);
        _ChatHistory.AddAssistant(response_message.ToString());
    }

    /// <summary>Настройка процесса json-сериализации</summary>
    internal static readonly JsonSerializerOptions JsonOptions = new(GptClientJsonSerializationContext.Default.Options) { Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin), };

    #endregion

    #region Генерация изображения

    private static readonly Regex __GuidRegex = GetGuidRegex();

    [GeneratedRegex("[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}")]
    private static partial Regex GetGuidRegex();

    public async Task<Guid> GenerateImageAsync(
        IEnumerable<Request> Requests,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        const string request_url = "chat/completions";

        ModelRequest message = new(Requests.ToArray(), Model, FunctionCall: "auto");

        var response = await Http.PostAsJsonAsync(request_url, message, JsonOptions, Cancel).ConfigureAwait(false);

        var response_message = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<ModelResponse>(cancellationToken: Cancel)
            .ConfigureAwait(false);

        var content_str = response_message.Choices.First(c => c.Message.Role == "assistant").Message.Content;
        var guid = Guid.Parse(__GuidRegex.Match(content_str).ValueSpan);

        return guid;
    }

    private async ValueTask<Stream> GetImageDownloadStreamAsync(Guid id, CancellationToken Cancel)
    {
        var response = await Http.GetAsync($"files/{id}/content", Cancel).ConfigureAwait(false);

        var stream = await response.EnsureSuccessStatusCode()
            .Content
            .ReadAsStreamAsync(Cancel)
            .ConfigureAwait(false);
        return stream;
    }

    public async Task<byte[]> DownloadImageById(Guid id, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);

        var result = new MemoryStream(new byte[stream.Length]);
        await stream.CopyToAsync(result, Cancel).ConfigureAwait(false);

        return result.ToArray();
    }

    public async Task DownloadImageById(Guid id, Func<Stream, Task> ProcessStream, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);
        await ProcessStream(stream).ConfigureAwait(false);
    }

    public async Task DownloadImageById(Guid id, Func<Stream, CancellationToken, Task> ProcessStream, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);
        await ProcessStream(stream, Cancel).ConfigureAwait(false);
    }

    public async Task<byte[]> GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        var result = await DownloadImageById(guid, Cancel).ConfigureAwait(false);
        return result;
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<byte[], Task> ProcessData,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        var result = await DownloadImageById(guid, Cancel).ConfigureAwait(false);
        await ProcessData(result).ConfigureAwait(false);
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<byte[], CancellationToken, Task> ProcessData,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        var result = await DownloadImageById(guid, Cancel).ConfigureAwait(false);
        await ProcessData(result, Cancel).ConfigureAwait(false);
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<Stream, Task> ProcessStream,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        await DownloadImageById(guid, ProcessStream, Cancel).ConfigureAwait(false);
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<Stream, CancellationToken, Task> ProcessStream,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        await DownloadImageById(guid, ProcessStream, Cancel).ConfigureAwait(false);
    }

    #endregion

    #region Векторизация текста

    /// <summary>Получить векторизацию текста</summary>
    /// <param name="MessageStrings">Строки, для которых требуется вычислить вектора</param>
    /// <param name="Model">Модель</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Результат векторизации текста</returns>
    public async Task<EmbeddingResponse> GetEmbeddingsAsync(
        IEnumerable<string> MessageStrings,
        EmbeddingModel Model = EmbeddingModel.Embeddings,
        CancellationToken Cancel = default)
    {
        const string url = "embeddings";

        var model_name = Model switch
        {
            EmbeddingModel.Embeddings => "Embeddings",
            EmbeddingModel.EmbeddingsGigaR => "EmbeddingsGigaR",
            _ => throw new InvalidEnumArgumentException("Неподдерживаемый тип модели", (int)Model, typeof(EmbeddingModel))
        };

        var request = new EmbeddingRequest(model_name, MessageStrings);

        var response = await Http.PostAsJsonAsync(url, request, JsonOptions, Cancel).ConfigureAwait(false);

        var result = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<EmbeddingResponse>(JsonOptions, Cancel)
            .ConfigureAwait(false);

        return result;
    }

    #endregion

    #region Баланс токенов

    /// <summary>Получить информацию о количестве токенов</summary>
    public async Task<BalanceInfoValue[]> GetTokensBalanceAsync(CancellationToken Cancel = default)
    {
        const string url = "balance";

        try
        {
            _Log.LogInformation("Запрос количества токенов");
            var balance = await Http.GetFromJsonAsync<BalanceInfo>(url, JsonOptions, Cancel).ConfigureAwait(false);

            _Log.LogTrace("Оставшееся количество токенов {Balance}", balance);

            return balance.Tokens;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.WriteLine(e);
            throw;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Forbidden)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion
}