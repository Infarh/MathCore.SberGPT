using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

using MathCore.SberGPT.Models;

using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public partial class GptClient
{
    #region Запрос модели

    /// <summary>Запрос модели</summary>
    /// <param name="UserPrompt">Запрос пользователя</param>
    /// <param name="SystemPrompt">Системный запрос</param>
    /// <param name="ChatHistory">История запросов</param>
    /// <param name="ModelName">Название модели</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Результат</returns>
    public async Task<Response> RequestAsync(
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

        var response_message = await response.AsJsonAsync<Response>(JsonOptions, Cancel).ConfigureAwait(false);

        while (response_message.Choices is [{ FinishReason: "function_call", Msg: { FunctionCall: { Name: var func_name, Arguments: var args } function_call, FunctionsStateId: var call_id } }, ..])
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

            response_message = await response.AsJsonAsync<Response>(JsonOptions, Cancel).ConfigureAwait(false);
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
    /// <returns>Асинхронный поток сообщений <see cref="StreamingResponse"/> от модели</returns>
    /// <exception cref="ArgumentNullException">Возникает, если <paramref name="UserPrompt"/> равен null</exception>
    /// <exception cref="ArgumentException">Возникает, если <paramref name="UserPrompt"/> является пустой строкой</exception>
    /// <exception cref="HttpRequestException">Возникает при ошибках HTTP-запроса</exception>
    public async IAsyncEnumerable<StreamingResponse> RequestStreamingAsync(
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

            await using var stream = await response.AsStream(Cancel).ConfigureAwait(false);

            var reader = new StreamReader(stream);

            ResponseChoice.Message.FuncInfo? function_call_value = null;
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
                var msg = JsonSerializer.Deserialize<StreamingResponse>(data, JsonOptions);

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
    internal static readonly JsonSerializerOptions JsonOptions = new(Infrastructure.GptClientJsonSerializationContext.Default.Options) { Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin), };

    #endregion

}
