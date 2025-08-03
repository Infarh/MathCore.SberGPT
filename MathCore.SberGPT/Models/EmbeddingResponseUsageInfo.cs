using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация об использовании токенов для векторизации</summary>
/// <param name="Tokens">Количество токенов, использованных для обработки входного текста</param>
public readonly record struct EmbeddingResponseUsageInfo([property: JsonPropertyName("prompt_tokens")] int Tokens);