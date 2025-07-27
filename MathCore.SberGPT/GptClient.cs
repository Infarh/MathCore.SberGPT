using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using static MathCore.SberGPT.GptClient.EmbeddingResponse;
using static MathCore.SberGPT.GptClient.EmbeddingResponse.EmbeddingValue;
using static MathCore.SberGPT.GptClient.ModelResponse;
using static MathCore.SberGPT.GptClient.ModelResponse.ChoiceValue.MessageValue;
using static MathCore.SberGPT.GptClient.StreamingResponseMessage;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace MathCore.SberGPT;

/// <summary>Клиент для запросов к Giga chat</summary>
/// <remarks>Клиент для запросов к Giga chat</remarks>
/// <param name="Http">Http-клиент для отправки запросов</param>
/// <param name="Log">Логгер</param>
public partial class GptClient(HttpClient Http, ILogger<GptClient> Log)
{
    //internal const string BaseUrl = "http://localhost:8881";
    internal const string BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1/";
    internal const string AuthUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
    internal const string RequesIdHeader = "RqUID";
    internal const string ClientXIdHeader = "X-Client-ID";
    internal const string RequestXIdHeader = "X-Request-ID";
    internal const string SessionXIdHeader = "X-Session-ID";

    private readonly ILogger _Log = Log;

    #region Конструктор

    /// <summary>Клиент для запросов к Giga chat</summary>
    public GptClient(IConfiguration config, ILogger<GptClient>? Log = null)
        : this(
            Http: new(new SberGPTRequestHandler(config, Log ?? NullLogger<GptClient>.Instance)) { BaseAddress = new(BaseUrl) },
            Log: Log ?? NullLogger<GptClient>.Instance)
    {

    }

    private static IConfiguration GetConfig(string Secret, string? Scope) => new ConfigurationBuilder()
        .AddInMemoryCollection([new("secret", Secret), new("scope", Scope ?? "GIGACHAT_API_PERS")]).Build();

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

    /// <summary>Ответ сервера, содержащий список моделей</summary>
    /// <param name="Models">Список моделей сервиса</param>
    private readonly record struct ModelsInfosListResponse([property: JsonPropertyName("data")] ModelInfo[] Models);

    /// <summary>Информация о модели сервиса</summary>
    /// <param name="ModelId">Идентификатор модели</param>
    private readonly record struct ModelInfo([property: JsonPropertyName("id")] string ModelId);

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

    /// <summary>Запрос о количестве токенов</summary>
    /// <param name="Model">Тип модели</param>
    /// <param name="Input">Ввод пользователя, для которого надо рассчитать количество токенов</param>
    private readonly record struct GetTokensCountRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string[] Input);

    /// <summary>Информация о количестве токенов</summary>
    /// <param name="Tokens">Количество токенов</param>
    /// <param name="Characters">Количество символов</param>
    private readonly record struct TokensCountResponse(
        [property: JsonPropertyName("tokens")] int Tokens,
        [property: JsonPropertyName("characters")] int Characters);

    /// <summary>Информация о количестве токенов для указанной строки ввода</summary>
    /// <param name="Input">Строка ввода</param>
    /// <param name="Tokens">Количество токенов</param>
    /// <param name="Characters">Количество символов</param>
    public readonly record struct TokensCount(string Input, int Tokens, int Characters);

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

    /// <summary>Информация о количестве токенов и символов для набора входных строк</summary>
    /// <param name="Counts">Список информации о количестве токенов и символов для каждой строки</param>
    public readonly record struct TokensCountInfo(IReadOnlyList<TokensCount> Counts) : IReadOnlyList<TokensCount>
    {
        /// <summary>Неявное преобразование массива в TokensCountInfo</summary>
        public static implicit operator TokensCountInfo(TokensCount[] list) => new(list);

        /// <summary>Общее количество токенов для всех строк</summary>
        [JsonIgnore] public int Tokens => Counts.Sum(c => c.Tokens);
        /// <summary>Общее количество символов для всех строк</summary>
        [JsonIgnore] public int Characters => Counts.Sum(c => c.Characters);

        /// <summary>Объединённый текст всех входных строк</summary>
        [JsonIgnore] public string Input => Counts.Aggregate(new StringBuilder(), (s, c) => s.AppendLine(c.Input), s => s.Length == 0 ? string.Empty : s.ToString(0, s.Length - Environment.NewLine.Length));

        /// <inheritdoc/>
        IEnumerator<TokensCount> IEnumerable<TokensCount>.GetEnumerator() => Counts.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Counts).GetEnumerator();
        /// <inheritdoc/>
        int IReadOnlyCollection<TokensCount>.Count => Counts.Count;
        /// <inheritdoc/>
        public TokensCount this[int index] => Counts[index];
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

        var request = GetRequestMessageJson(HttpMethod.Post, url, message, __DefaultOptions);

        var response = await Http.SendAsync(request, Cancel).ConfigureAwait(false);

        var response_message = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<ModelResponse>(__DefaultOptions, Cancel)
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

            request = GetRequestMessageJson(HttpMethod.Post, url, message, __DefaultOptions);

            response = await Http.SendAsync(request, Cancel).ConfigureAwait(false);

            response_message = await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<ModelResponse>(__DefaultOptions, Cancel)
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
    /// <returns>Асинхронный поток сообщений <see cref="StreamingResponseMessage"/> от модели</returns>
    /// <exception cref="ArgumentNullException">Возникает, если <paramref name="UserPrompt"/> равен null</exception>
    /// <exception cref="ArgumentException">Возникает, если <paramref name="UserPrompt"/> является пустой строкой</exception>
    /// <exception cref="HttpRequestException">Возникает при ошибках HTTP-запроса</exception>
    public async IAsyncEnumerable<StreamingResponseMessage> RequestStreamingAsync(
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

            var request = GetRequestMessageJson(HttpMethod.Post, url, message, __DefaultOptions);

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

            FunctionCallValue? function_call_value = null;
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
                var msg = JsonSerializer.Deserialize<StreamingResponseMessage>(data, __DefaultOptions);

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

    /// <summary>Сообщение запроса к модели</summary>
    /// <param name="Requests">Массив элементов запроса</param>
    /// <param name="Model">Тип модели</param>
    /// <param name="FunctionCall">
    /// Определяет режим вызова функций: none, auto<br/>
    /// - <b>none</b> - запрет на вызов любых функций<br/>
    /// - <b>auto</b> - вызов внутренних функций и, если указано поле functions, то вызов пользовательских функций<br/>
    /// - объект {"name": "имя_функции"}<br/>
    /// </param>
    /// <param name="FunctionSchemes">Список схем функций, доступных модели для вызова</param>
    /// <param name="Streaming">Использовать потоковую передачу (по умолчанию false)</param>
    /// <param name="UpdateInterval">Интервал в секундах отправки результатов при потоковой передаче</param>
    /// <param name="Temperature">Величина температуры. Должна быть больше 0. Чем выше значение, тем более случайным будет ответ.</param>
    /// <param name="TemperatureAlternative">Альтернативное значение температуры. Значение от 0 до 1. Определяет процент используемых токенов запроса.</param>
    /// <param name="MaxTokensCount">Максимальное количество токенов, которые будут использованы для создания ответов</param>
    /// <param name="RepetitionPenalty">Количество повторений слов. По умолчанию 1.0. При значении больше 1 модель будет стараться не повторять слова.</param>
    internal readonly record struct ModelRequest(
        [property: JsonPropertyName("messages")] Request[] Requests,
        [property: JsonPropertyName("model")] string Model = "GigaChat-2",
        [property: JsonPropertyName("function_call")] string? FunctionCall = "auto",
        [property: JsonPropertyName("functions")] IEnumerable<JsonNode>? FunctionSchemes = null,
        [property: JsonPropertyName("temperature")] double? Temperature = null,
        [property: JsonPropertyName("top_p")] double? TemperatureAlternative = null,
        [property: JsonPropertyName("stream")] bool? Streaming = null,
        [property: JsonPropertyName("max_tokens")] int? MaxTokensCount = null,
        [property: JsonPropertyName("repetition_penalty")] double? RepetitionPenalty = null,
        [property: JsonPropertyName("update_interval")] int? UpdateInterval = null
        );

    /// <summary>Сообщение пользователя модели</summary>
    /// <param name="Content">Текст сообщения</param>
    /// <param name="Role">
    /// Тип рои: system, user, assistant, function<br/>
    /// - system — системный промт, который задает роль модели, например, должна модель отвечать как академик или как школьник;<br/>
    /// - assistant — ответ модели;<br/>
    /// - user — сообщение пользователя;<br/>
    /// - function — сообщение с результатом работы пользовательской функции. В сообщении с этой ролью передавайте в поле content валидный JSON-объект с результатами работы функции.
    /// </param>
    public readonly record struct Request(
        [property: JsonPropertyName("content"), JsonPropertyOrder(1)] string Content,
        [property: JsonPropertyName("role"), JsonPropertyOrder(0)] RequestRole Role = RequestRole.user,
        [property: JsonPropertyName("functions_state_id")] Guid? FunctionStateId = null,
        [property: JsonPropertyName("function_call")] FunctionCallValue? FunctionCall = null,
        [property: JsonPropertyName("name")] string? Name = null
        )
    {
        /// <summary>Создаёт запрос пользователя с текстовым содержимым</summary>
        /// <param name="Content">Текст сообщения пользователя</param>
        /// <returns>Объект запроса с ролью пользователя</returns>
        public static Request User(string Content) => new(Content);

        /// <summary>Создаёт запрос ассистента с текстовым содержимым</summary>
        /// <param name="Content">Текст ответа ассистента</param>
        /// <returns>Объект запроса с ролью ассистента</returns>
        public static Request Assistant(string Content) => new(Content, RequestRole.assistant);

        /// <summary>Создаёт системный запрос для задания роли модели</summary>
        /// <param name="Content">Текст системного промпта</param>
        /// <returns>Объект запроса с системной ролью</returns>
        public static Request System(string Content) => new(Content, RequestRole.system);

        /// <summary>Создаёт запрос с результатом работы функции</summary>
        /// <param name="Content">JSON-объект с результатами выполнения функции</param>
        /// <returns>Объект запроса с ролью функции</returns>
        public static Request Function(string Content) => new(Content, RequestRole.function);

        /// <summary>Оператор неявного преобразования кортежа, содержащего текст запроса и роль в объект запроса</summary>
        /// <param name="request">Объект запроса</param>
        public static implicit operator Request((string Content, RequestRole Role) request) => new(request.Content, request.Role);

        public static implicit operator Request(string request) => new(request);
    }

    /// <summary>Ответ модели</summary>
    /// <param name="Choices">Ответы модели</param>
    /// <param name="CreatedUnixTime">Время формирования ответа</param>
    /// <param name="Model">Тип модели</param>
    /// <param name="Usage">Данные об использовании модели</param>
    /// <param name="CallMethodName">Название вызываемого метода</param>
    public readonly record struct ModelResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChoiceValue> Choices,
        [property: JsonPropertyName("created")] int CreatedUnixTime,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("usage")] UsageValue Usage,
        [property: JsonPropertyName("object")] string? CallMethodName
        )
    {
        /// <summary>Время формирования ответа</summary>
        [JsonIgnore]
        public DateTimeOffset CreateTime => DateTimeOffset.UnixEpoch.AddSeconds(CreatedUnixTime / 1000d);

        public DateTimeOffset CreateTime2 => DateTimeOffset.FromUnixTimeMilliseconds(CreatedUnixTime);

        public IEnumerable<string> AssistMessages => Choices
            .Where(c => c.Message.Role == "assistant")
            .Select(c => c.Message.Content);

        public string AssistMessage => AssistMessages.ToSeparatedStr(Environment.NewLine);

        /// <summary>Ответ модели</summary>
        /// <param name="Message">Сгенерированное сообщение</param>
        /// <param name="Index">Индекс сообщения в массиве начиная с ноля</param>
        /// <param name="FinishReason">
        /// Причина завершения гипотезы. Возможные значения:<br/>
        /// stop — модель закончила формировать гипотезу и вернула полный ответ;<br/>
        /// length — достигнут лимит токенов в сообщении;<br/>
        /// function_call — указывает что при запросе была вызвана встроенная функция или сгенерированы аргументы для пользовательской функции;<br/>
        /// blacklist — запрос подпадает под тематические ограничения.
        /// </param>
        public readonly record struct ChoiceValue(
            [property: JsonPropertyName("message")] ChoiceValue.MessageValue Message,
            [property: JsonPropertyName("index")] int Index,
            [property: JsonPropertyName("finish_reason")] string FinishReason
        )
        {
            /// <summary>Расшифровка информации о причине завершения запроса</summary>
            [JsonIgnore]
            public string FinishReasonInfo => FinishReason switch
            {
                "stop" => "Модель закончила формировать гипотезу и вернула полный ответ",
                "length" => "Достигнут лимит токенов в сообщении",
                "function_call" => "Указывает что при запросе была вызвана встроенная функция или сгенерированы аргументы для пользовательской функции",
                "blacklist" => "Запрос подпадает под тематические ограничения",
                _ => throw new ArgumentOutOfRangeException(nameof(FinishReason), FinishReason, "Значение должно быть одним из: stop, length, function_call, blacklist")
            };

            /// <summary>Сгенерированное сообщение</summary>
            /// <param name="Role">
            /// Роль автора сообщения. Возможные значения: assistant, function_in_progress<br/>
            /// Роль function_in_progress используется при работе встроенных функций в режиме потоковой передачи токенов.
            /// </param>
            /// <param name="Content">
            /// Содержимое сообщения, например, результат генерации.<br/>
            /// В сообщениях с ролью function_in_progress содержит информацию о том, сколько времени осталось до завершения работы встроенной функции.
            /// </param>
            public readonly record struct MessageValue(
                [property: JsonPropertyName("role")] string Role,
                [property: JsonPropertyName("content")] string Content,
                [property: JsonPropertyName("function_call")] MessageValue.FunctionCallValue FunctionCall,
                [property: JsonPropertyName("functions_state_id")] Guid FunctionsStateId
            )
            {
                /// <summary>Информация о вызове функции</summary>
                /// <param name="Name">Название функции</param>
                /// <param name="Arguments">Перечень аргументов функции с их значениями</param>
                public readonly record struct FunctionCallValue(
                    [property: JsonPropertyName("name")] string Name,
                    [property: JsonPropertyName("arguments")] JsonObject Arguments
                );
            }
        }

        /// <summary>Информация об использовании токенов модели</summary>
        /// <param name="PromptTokens">Количество токенов в промпте запроса</param>
        /// <param name="CompletionTokens">Количество токенов в ответе модели</param>
        /// <param name="PrecachedPromptTokens">Количество предварительно кэшированных токенов промпта</param>
        /// <param name="TotalTokens">Общее количество использованных токенов</param>
        public readonly record struct UsageValue(
            [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
            [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
            [property: JsonPropertyName("precached_prompt_tokens")] int PrecachedPromptTokens,
            [property: JsonPropertyName("total_tokens")] int TotalTokens
        );

        public override string ToString()
        {
            const int max_length = 60;

            var assist_msg = new StringBuilder();
            foreach (var msg in AssistMessages)
            {
                assist_msg.AppendLine(msg);
                if (assist_msg.Length > max_length)
                    break;
            }

            assist_msg.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

            if (assist_msg.Length > max_length)
            {
                assist_msg.Length = max_length - 3;
                assist_msg.Append("...");
            }

            return $"assist: {assist_msg} tokens: {Usage.TotalTokens} ({Usage.PrecachedPromptTokens})";
        }

        public static implicit operator string(ModelResponse response) => response.AssistMessage;
    }

    /// <summary>Настройка процесса json-сериализации</summary>
    private static readonly JsonSerializerOptions __DefaultOptions = new(GptClientJsonSerializationContext.Default.Options) { Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin), };

    /// <summary>Сообщение потокового ответа от модели</summary>
    /// <param name="Choices">Список фрагментов ответа модели</param>
    /// <param name="CreatedUnixTime">Время создания ответа в Unix timestamp</param>
    /// <param name="Model">Название модели, сгенерировавшей ответ</param>
    /// <param name="CallMethodName">Название вызываемого метода API</param>
    public readonly record struct StreamingResponseMessage(
        [property: JsonPropertyName("choices")] IReadOnlyList<StreamingChoiceValue> Choices,
        [property: JsonPropertyName("created")] int CreatedUnixTime,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("object")] string? CallMethodName
    )
    {
        public FunctionCallValue? FunctionCall => Choices.FirstOrDefault(s => s.Delta.FunctionCall is not null).Delta.FunctionCall;

        public Guid? FunctionCallStateId => Choices.FirstOrDefault(s => s.Delta.FunctionsStateId is not null).Delta.FunctionsStateId;

        /// <summary>Фрагмент потокового ответа модели</summary>
        /// <param name="Delta">Дельта-сообщение с частичным содержимым</param>
        /// <param name="Index">Индекс фрагмента в потоке сообщений</param>
        public readonly record struct StreamingChoiceValue(
            [property: JsonPropertyName("delta")] StreamingChoiceValue.DeltaValue Delta,
            [property: JsonPropertyName("index")] int Index
        )
        {
            /// <summary>Дельта-содержимое потокового сообщения</summary>
            /// <param name="Content">Частичное текстовое содержимое сообщения</param>
            /// <param name="Role">Роль отправителя сообщения (system, user, assistant, function)</param>
            public readonly record struct DeltaValue(
                [property: JsonPropertyName("content")] string Content,
                [property: JsonPropertyName("role")] string Role,
                [property: JsonPropertyName("created"), JsonConverter(typeof(UnixDateTimeConverter))] DateTime? Created,
                [property: JsonPropertyName("functions_state_id")] Guid? FunctionsStateId,
                [property: JsonPropertyName("model")] string? Model,
                [property: JsonPropertyName("object")] string? Object,
                [property: JsonPropertyName("function_call")] FunctionCallValue? FunctionCall
            );
        }

        /// <summary>Объединенное сообщение из всех фрагментов контента</summary>
        public string Message => string.Concat(Choices.Select(c => c.Delta.Content));

        /// <summary>Объединенное сообщение только от ассистента из всех фрагментов контента</summary>
        public string MessageAssistant => string.Concat(Choices.Where(c => c.Delta.Role == "assistant").Select(c => c.Delta.Content));
    }

    #endregion

    #region Генерация изображения

    /// <summary></summary>
    private static readonly Regex __GuidRegex = GetGuidRegex();

    /// <summary></summary>
    [GeneratedRegex("[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}")]
    private static partial Regex GetGuidRegex();

    /// <summary></summary>
    /// <param name="Requests"></param>
    /// <param name="Model"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
    public async Task<Guid> GenerateImageAsync(
        IEnumerable<Request> Requests,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        const string request_url = "chat/completions";

        ModelRequest message = new(Requests.ToArray(), Model, FunctionCall: "auto");

        var response = await Http.PostAsJsonAsync(request_url, message, __DefaultOptions, Cancel).ConfigureAwait(false);

        var response_message = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<ModelResponse>(cancellationToken: Cancel)
            .ConfigureAwait(false);

        var content_str = response_message.Choices.First(c => c.Message.Role == "assistant").Message.Content;
        var guid = Guid.Parse(__GuidRegex.Match(content_str).ValueSpan);

        return guid;
    }

    /// <summary></summary>
    /// <param name="id"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
    private async ValueTask<Stream> GetImageDownloadStreamAsync(Guid id, CancellationToken Cancel)
    {
        var response = await Http.GetAsync($"files/{id}/content", Cancel).ConfigureAwait(false);

        var stream = await response.EnsureSuccessStatusCode()
            .Content
            .ReadAsStreamAsync(Cancel)
            .ConfigureAwait(false);
        return stream;
    }

    /// <summary></summary>
    /// <param name="id"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
    public async Task<byte[]> DownloadImageById(Guid id, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);

        var result = new MemoryStream(new byte[stream.Length]);
        await stream.CopyToAsync(result, Cancel).ConfigureAwait(false);

        return result.ToArray();
    }

    /// <summary></summary>
    /// <param name="id"></param>
    /// <param name="ProcessStream"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
    public async Task DownloadImageById(Guid id, Func<Stream, Task> ProcessStream, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);
        await ProcessStream(stream).ConfigureAwait(false);
    }

    /// <summary></summary>
    /// <param name="id"></param>
    /// <param name="ProcessStream"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
    public async Task DownloadImageById(Guid id, Func<Stream, CancellationToken, Task> ProcessStream, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);
        await ProcessStream(stream, Cancel).ConfigureAwait(false);
    }

    /// <summary></summary>
    /// <param name="Requests"></param>
    /// <param name="Model"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
    public async Task<byte[]> GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        var result = await DownloadImageById(guid, Cancel).ConfigureAwait(false);
        return result;
    }

    /// <summary></summary>
    /// <param name="Requests"></param>
    /// <param name="ProcessData"></param>
    /// <param name="Model"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
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

    /// <summary></summary>
    /// <param name="Requests"></param>
    /// <param name="ProcessData"></param>
    /// <param name="Model"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
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

    /// <summary></summary>
    /// <param name="Requests"></param>
    /// <param name="ProcessStream"></param>
    /// <param name="Model"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<Stream, Task> ProcessStream,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        await DownloadImageById(guid, ProcessStream, Cancel).ConfigureAwait(false);
    }

    /// <summary></summary>
    /// <param name="Requests"></param>
    /// <param name="ProcessStream"></param>
    /// <param name="Model"></param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns></returns>
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

    internal readonly record struct EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IEnumerable<string> Input
    );

    /// <summary>Модели для векторизации текста</summary>
    public enum EmbeddingModel
    {
        /// <summary>Стандартная модель векторизации</summary>
        Embeddings,
        /// <summary>Модель векторизации GigaR</summary>
        EmbeddingsGigaR
    }

    /// <summary>Ответ сервера с информацией о векторизации текста</summary>
    /// <param name="ListIdStr">Идентификатор объекта списка</param>
    /// <param name="Values">Массив векторных представлений входных текстов</param>
    public readonly record struct EmbeddingResponse(
        [property: JsonPropertyName("object")] string ListIdStr,
        [property: JsonPropertyName("data")] EmbeddingValue[] Values
    )
    {
        /// <summary>Векторное представление входного текста</summary>
        /// <param name="EmbeddingStr">Тип объекта векторизации</param>
        /// <param name="Embedding">Массив векторных значений для входного текста</param>
        /// <param name="Index">Индекс элемента в массиве векторизации</param>
        /// <param name="Usage">Информация об использовании токенов при векторизации</param>
        public readonly record struct EmbeddingValue(
            [property: JsonPropertyName("object")] string EmbeddingStr,
            [property: JsonPropertyName("embedding")] double[] Embedding,
            [property: JsonPropertyName("index")] int Index,
            [property: JsonPropertyName("usage")] UsageInfo Usage
        )
        {
            /// <summary>Информация об использовании токенов для векторизации</summary>
            /// <param name="Tokens">Количество токенов, использованных для обработки входного текста</param>
            public readonly record struct UsageInfo([property: JsonPropertyName("prompt_tokens")] int Tokens);

            /// <summary>Количество токенов, использованных для векторизации</summary>
            public int Tokens => Usage.Tokens;
        }
    }


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

        var response = await Http.PostAsJsonAsync(url, request, __DefaultOptions, Cancel).ConfigureAwait(false);

        var result = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<EmbeddingResponse>(__DefaultOptions, Cancel)
            .ConfigureAwait(false);

        return result;
    }

    #endregion

    #region Баланс токенов

    /// <summary>Информация о количестве токенов модели</summary>
    /// <param name="Model">Название модели</param>
    /// <param name="TokensElapsed">Количество токенов</param>
    public readonly record struct BalanceValue(
        [property: JsonPropertyName("usage")] string Model,
        [property: JsonPropertyName("value")] int TokensElapsed
    )
    {
        public override string ToString() => $"{Model}:{TokensElapsed}";
    }

    /// <summary>Информация о количестве токенов</summary>
    /// <param name="Tokens">Перечень значений количеств оставшихся токенов по моделям</param>
    public readonly record struct BalanceInfo(
        [property: JsonPropertyName("balance")] BalanceValue[] Tokens
    )
    {
        public override string ToString() => string.Join("; ", Tokens);
    }

    /// <summary>Получить информацию о количестве токенов</summary>
    public async Task<BalanceValue[]> GetTokensBalanceAsync(CancellationToken Cancel = default)
    {
        const string url = "balance";

        try
        {
            _Log.LogInformation("Запрос количества токенов");
            var balance = await Http.GetFromJsonAsync<BalanceInfo>(url, __DefaultOptions, Cancel).ConfigureAwait(false);

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