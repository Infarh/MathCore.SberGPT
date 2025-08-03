using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Фрагмент потокового ответа модели</summary>
/// <param name="Delta">Дельта-сообщение с частичным содержимым</param>
/// <param name="Index">Индекс фрагмента в потоке сообщений</param>
public readonly record struct StreamingResponseMsgChoice(
    [property: JsonPropertyName("delta")] StreamingResponseMsgChoiceDelta Delta,
    [property: JsonPropertyName("index")] int Index
);