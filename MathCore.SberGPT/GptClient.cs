using System.Collections;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using static MathCore.SberGPT.GptClient.ModelResponse;
using static MathCore.SberGPT.GptClient.TokensCount;

namespace MathCore.SberGPT;

/// <summary>Клиент для запросов к Giga chat</summary>
/// <param name="http">Http-клиент для отправки запросов</param>
public partial class GptClient(HttpClient http)
{
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

        var response = await http
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
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat"</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Информация о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<TokensCount> GetTokensCountAsync(string Input, string Model = "GigaChat", CancellationToken Cancel = default)
    {
        var tokens = await GetTokensCountAsync([Input], Model, Cancel).ConfigureAwait(false);
        return tokens[0];
    }

    public readonly record struct TokensCountInfo(IReadOnlyList<TokensCount> Counts) : IReadOnlyList<TokensCount>
    {
        public static implicit operator TokensCountInfo(TokensCount[] list) => new(list);

        [JsonIgnore] public int Tokens => Counts.Sum(c => c.Tokens);
        [JsonIgnore] public int Characters => Counts.Sum(c => c.Characters);

        [JsonIgnore] public string Input => Counts.Aggregate(new StringBuilder(), (S, s) => S.AppendLine(s.Input), s => s.Length == 0 ? string.Empty : s.ToString(0, s.Length - Environment.NewLine.Length));

        IEnumerator<TokensCount> IEnumerable<TokensCount>.GetEnumerator() => Counts.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Counts).GetEnumerator();

        int IReadOnlyCollection<TokensCount>.Count => Counts.Count;

        public TokensCount this[int index] => Counts[index];
    }

    /// <summary>Получить количество токенов для указанной строки ввода</summary>
    /// <param name="Input">Строки ввода</param>
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat"</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Массив с информацией о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<TokensCountInfo> GetTokensCountAsync(IEnumerable<string> Input, string Model = "GigaChat", CancellationToken Cancel = default)
    {
        var input = Input.ToArray();
        const string url = "tokens/count";
        var response = await http
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

    /// <summary>Сообщение запроса к модели</summary>
    /// <param name="Requests">Массив элементов запроса</param>
    /// <param name="Model">Тип модели</param>
    /// <param name="FunctionCall">
    /// Определяет режим вызова функций: none, auto<br/>
    /// - <b>none</b> - запрет на вызов любых функций<br/>
    /// - <b>auto</b> - вызов внутренних функций и, если указано поле functions, то вызов пользовательских функций
    /// </param>
    /// <param name="Streaming">Использовать потоковую передачу</param>
    /// <param name="UpdateInterval">Интервал в секундах отправки результатов при потоковой передаче</param>
    /// <param name="Temperature">Величина температуры. Должна быть больше 0. Чем выше значение, тем более случайным будет ответ.</param>
    /// <param name="TemperatureAlternative">Альтернативное значение температуры. Значение от 0 до 1. Определяет процент используемых токенов запроса.</param>
    /// <param name="MaxTokensCount">Максимальное количество токенов, которые будут использованы для создания ответов</param>
    /// <param name="RepetitionPenalty">Количество повторений слов. По умолчанию 1.0. При значении больше 1 модель будет стараться не повторять слова.</param>
    internal readonly record struct ModelRequest(
        [property: JsonPropertyName("messages")] Request[] Requests,
        [property: JsonPropertyName("model")] string Model = "GigaChat",
        [property: JsonPropertyName("function_call")] string? FunctionCall = "auto",
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
    /// - system — системный промпт, который задает роль модели, например, должна модель отвечать как академик или как школьник;<br/>
    /// - assistant — ответ модели;<br/>
    /// - user — сообщение пользователя;<br/>
    /// - function — сообщение с результатом работы пользовательской функции. В сообщении с этой ролью передавайте в поле content валидный JSON-объект с результатами работы функции.
    /// </param>
    public readonly record struct Request(
        [property: JsonPropertyName("content"), JsonPropertyOrder(1)] string Content,
        [property: JsonPropertyName("role"), JsonPropertyOrder(0)] RequestRole Role = RequestRole.user)
    {
        /// <summary>Оператор неявного преобразования кортежа, содержащего текст запроса и роль в объект запроса</summary>
        /// <param name="request">Объект запроса</param>
        public static implicit operator Request((string Content, RequestRole Role) request) => new(request.Content, request.Role);
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
        [property: JsonPropertyName("_object")] string? CallMethodName
        )
    {
        /// <summary>Время формирования ответа</summary>
        [JsonIgnore]
        public DateTimeOffset CreateTime => DateTimeOffset.UnixEpoch.AddSeconds(CreatedUnixTime / 1000d);

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
            /// <param name="DataForContext">Массив сообщений, описывающих работу встроенных функций</param>
            public readonly record struct MessageValue(
                [property: JsonPropertyName("role")] string Role,
                [property: JsonPropertyName("content")] string Content,
                [property: JsonPropertyName("data_for_context")] IReadOnlyList<MessageValue.DataForContextValue> DataForContext
            )
            {
                /// <summary>Информация о работе встроенной функции</summary>
                /// <param name="Content"></param>
                /// <param name="Role"></param>
                /// <param name="FunctionCall"></param>
                public readonly record struct DataForContextValue(
                    [property: JsonPropertyName("content")] string Content,
                    [property: JsonPropertyName("role")] string Role,
                    [property: JsonPropertyName("function_call")] DataForContextValue.FunctionCallValue? FunctionCall
                )
                {
                    /// <summary></summary>
                    /// <param name="Name"></param>
                    /// <param name="Arguments"></param>
                    public readonly record struct FunctionCallValue(
                        [property: JsonPropertyName("name")] string Name,
                        [property: JsonPropertyName("arguments")] IReadOnlyDictionary<string, string> Arguments
                        );
                }
            }
        }

        /// <summary></summary>
        public readonly record struct UsageValue(
            [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
            [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
            [property: JsonPropertyName("total_tokens")] int TotalTokens
        );
    }

    /// <summary>Настройка процесса json-сериализации</summary>
    private static readonly JsonSerializerOptions __DefaultOptions = new(GptClientJsonSerializationContext.Default.Options) { Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin), };

    /// <summary>Запрос модели</summary>
    /// <param name="Requests">Набор параметров запроса</param>
    /// <param name="Model">Название модели</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Результат</returns>
    public async Task<IEnumerable<string>> RequestAsync(
        IEnumerable<Request> Requests,
        string Model = "GigaChat",
        //bool Streaming = false,
        //int UpdateInterval = 0,
        CancellationToken Cancel = default)
    {
        const string url = "chat/completions";

        ModelRequest message = new(Requests.ToArray(), Model);

        var response = await http
            .PostAsJsonAsync(url, message, __DefaultOptions, Cancel)
            .ConfigureAwait(false);

        var response_message = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<ModelResponse>(cancellationToken: Cancel)
            .ConfigureAwait(false);

        return response_message.Choices
            .Where(c => c.Message.Role is "assistant")
            .Select(c => c.Message.Content);
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
        string Model = "GigaChat",
        CancellationToken Cancel = default)
    {
        const string request_url = "chat/completions";

        ModelRequest message = new(Requests.ToArray(), Model, FunctionCall: "auto");

        var response = await http.PostAsJsonAsync(request_url, message, __DefaultOptions, Cancel).ConfigureAwait(false);

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
        var response = await http.GetAsync($"files/{id}/content", Cancel).ConfigureAwait(false);

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
        string Model = "GigaChat",
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
        string Model = "GigaChat",
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
        string Model = "GigaChat",
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
        string Model = "GigaChat",
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
        string Model = "GigaChat",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        await DownloadImageById(guid, ProcessStream, Cancel).ConfigureAwait(false);
    }

    #endregion

    #region Векторизация текста



    #endregion
}