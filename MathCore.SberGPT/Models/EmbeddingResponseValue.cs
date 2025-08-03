using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Векторное представление входного текста</summary>
/// <param name="EmbeddingStr">Тип объекта векторизации</param>
/// <param name="Embedding">Массив векторных значений для входного текста</param>
/// <param name="Index">Индекс элемента в массиве векторизации</param>
/// <param name="Usage">Информация об использовании токенов при векторизации</param>
public readonly record struct EmbeddingResponseValue(
    [property: JsonPropertyName("object")] string EmbeddingStr,
    [property: JsonPropertyName("embedding")] double[] Embedding,
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("usage")] EmbeddingResponseUsageInfo Usage
)
{
    /// <summary>Количество токенов, использованных для векторизации</summary>
    public int Tokens => Usage.Tokens;
}