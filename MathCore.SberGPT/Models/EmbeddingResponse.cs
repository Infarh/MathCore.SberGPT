using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Ответ сервера с информацией о векторизации текста</summary>
/// <param name="ListIdStr">Идентификатор объекта списка</param>
/// <param name="Values">Массив векторных представлений входных текстов</param>
public readonly record struct EmbeddingResponse(
    [property: JsonPropertyName("object")] string ListIdStr,
    [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingResponseValue> Values
    );