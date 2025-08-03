using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Дельта-содержимое потокового сообщения</summary>
/// <param name="Content">Частичное текстовое содержимое сообщения</param>
/// <param name="Role">Роль отправителя сообщения (system, user, assistant, function)</param>
public readonly record struct StreamingResponseMsgChoiceDelta(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("created"), JsonConverter(typeof(UnixDateTimeConverter))] DateTime? Created,
    [property: JsonPropertyName("functions_state_id")] Guid? FunctionsStateId,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("object")] string? Object,
    [property: JsonPropertyName("function_call")] ResponseChoiceMsgFunc? FunctionCall
);