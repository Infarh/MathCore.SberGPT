using System.Runtime.CompilerServices;

using static MathCore.SberGPT.GptClient.ModelResponse.ChoiceValue.MessageValue;

namespace MathCore.SberGPT.Infrastructure.Extensions;

/// <summary>Методы-расширения для работы со списком запросов GPT-клиента</summary>
internal static class ListOfRequestsEx
{
    /// <summary>Добавляет сообщение ассистента в список запросов</summary>
    /// <param name="requests">Список запросов</param>
    /// <param name="AssistMessage">Текст сообщения ассистента</param>
    public static void AddAssistant(this List<GptClient.Request> requests, string AssistMessage) => requests.Add(GptClient.Request.Assistant(AssistMessage));

    /// <summary>Добавляет сообщение пользователя в список запросов</summary>
    /// <param name="requests">Список запросов</param>
    /// <param name="UserPrompt">Текст сообщения пользователя</param>
    public static void AddUser(this List<GptClient.Request> requests, string UserPrompt) => requests.Add(GptClient.Request.User(UserPrompt));

    /// <summary>Добавляет системное сообщение в список запросов, если оно не пустое</summary>
    /// <param name="requests">Список запросов</param>
    /// <param name="SystemPrompt">Текст системного сообщения</param>
    public static void AddSystem(this List<GptClient.Request> requests, string? SystemPrompt)
    {
        if (string.IsNullOrWhiteSpace(SystemPrompt))
            return;
        requests.Add(GptClient.Request.System(SystemPrompt));
    }

    /// <summary>Добавляет историю сообщений в список запросов</summary>
    /// <param name="requests">Список запросов</param>
    /// <param name="History">История сообщений</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddHistory(this List<GptClient.Request> requests, IEnumerable<GptClient.Request>? History)
    {
        if (History is null) return;
        requests.AddRange(History);
    }

    /// <summary>Добавляет вызов функции и результат её выполнения в список запросов</summary>
    /// <param name="requests">Список запросов</param>
    /// <param name="InvokeResultJson">Результат вызова функции в формате JSON</param>
    /// <param name="CallId">Идентификатор вызова функции</param>
    /// <param name="FunctionName">Имя функции</param>
    /// <param name="FunctionCall">Параметры вызова функции</param>
    public static void AddFunctionCall(
        this List<GptClient.Request> requests,
        string InvokeResultJson,
        Guid CallId,
        string FunctionName,
        FunctionCallValue FunctionCall)
    {
        requests.Add(new(InvokeResultJson, RequestRole.assistant, FunctionStateId: CallId, FunctionCall: FunctionCall));
        requests.Add(new(InvokeResultJson, RequestRole.function, Name: FunctionName));
    }
}
