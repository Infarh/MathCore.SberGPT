using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Сообщение потокового ответа от модели</summary>
/// <param name="Choices">Список фрагментов ответа модели</param>
/// <param name="CreatedUnixTime">Время создания ответа в Unix timestamp</param>
/// <param name="Model">Название модели, сгенерировавшей ответ</param>
/// <param name="CallMethodName">Название вызываемого метода API</param>
public readonly record struct StreamingResponseMsg(
    [property: JsonPropertyName("choices")] IReadOnlyList<StreamingResponseMsgChoice> Choices,
    [property: JsonPropertyName("created")] int CreatedUnixTime,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("object")] string? CallMethodName
)
{
    public ResponseChoiceMsgFunc? FunctionCall => Choices.FirstOrDefault(s => s.Delta.FunctionCall is not null).Delta.FunctionCall;

    public Guid? FunctionCallStateId => Choices.FirstOrDefault(s => s.Delta.FunctionsStateId is not null).Delta.FunctionsStateId;

    /// <summary>Объединенное сообщение из всех фрагментов контента</summary>
    public string Message => string.Concat(Choices.Select(c => c.Delta.Content));

    /// <summary>Объединенное сообщение только от ассистента из всех фрагментов контента</summary>
    public string MessageAssistant => string.Concat(Choices.Where(c => c.Delta.Role == "assistant").Select(c => c.Delta.Content));
}