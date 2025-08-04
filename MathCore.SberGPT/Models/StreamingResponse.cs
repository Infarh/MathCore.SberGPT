using System.Text.Json.Serialization;

using MathCore.SberGPT.Infrastructure;

namespace MathCore.SberGPT.Models;

/// <summary>Сообщение потокового ответа от модели</summary>
/// <param name="Choices">Список фрагментов ответа модели</param>
/// <param name="CreatedUnixTime">Время создания ответа в Unix timestamp</param>
/// <param name="Model">Название модели, сгенерировавшей ответ</param>
/// <param name="CallMethodName">Название вызываемого метода API</param>
public readonly record struct StreamingResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<StreamingResponse.Choice> Choices
    , [property: JsonPropertyName("created")] int CreatedUnixTime
    , [property: JsonPropertyName("model")] string Model
    , [property: JsonPropertyName("object")] string? CallMethodName
    )
{
    /// <summary>Фрагмент потокового ответа модели</summary>
    /// <param name="Delta">Дельта-сообщение с частичным содержимым</param>
    /// <param name="Index">Индекс фрагмента в потоке сообщений</param>
    public readonly record struct Choice(
        [property: JsonPropertyName("delta")] Choice.ChoiceDelta Delta,
        [property: JsonPropertyName("index")] int Index
    )
    {
        /// <summary>Дельта-содержимое потокового сообщения</summary>
        /// <param name="Content">Частичное текстовое содержимое сообщения</param>
        /// <param name="Role">Роль отправителя сообщения (system, user, assistant, function)</param>
        public readonly record struct ChoiceDelta(
            [property: JsonPropertyName("content")] string Content
            , [property: JsonPropertyName("role")] string Role
            , [property: JsonPropertyName("created"), JsonConverter(typeof(UnixDateTimeConverter))] DateTime? Created
            , [property: JsonPropertyName("functions_state_id")] Guid? FunctionsStateId
            , [property: JsonPropertyName("model")] string? Model
            , [property: JsonPropertyName("object")] string? Object
            , [property: JsonPropertyName("function_call")] ResponseChoice.Message.FuncInfo? FunctionCall
        );
    }

    public ResponseChoice.Message.FuncInfo? FunctionCall => Choices.FirstOrDefault(s => s.Delta.FunctionCall is not null).Delta.FunctionCall;

    public Guid? FunctionCallStateId => Choices.FirstOrDefault(s => s.Delta.FunctionsStateId is not null).Delta.FunctionsStateId;

    /// <summary>Объединенное сообщение из всех фрагментов контента</summary>
    public string Message => string.Concat(Choices.Select(c => c.Delta.Content));

    /// <summary>Объединенное сообщение только от ассистента из всех фрагментов контента</summary>
    public string MessageAssistant => string.Concat(Choices.Where(c => c.Delta.Role == "assistant").Select(c => c.Delta.Content));
}