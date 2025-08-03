using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация об использовании токенов модели</summary>
/// <param name="PromptTokens">Количество токенов в промпте запроса</param>
/// <param name="CompletionTokens">Количество токенов в ответе модели</param>
/// <param name="PrecachedPromptTokens">Количество предварительно кэшированных токенов промпта</param>
/// <param name="TotalTokens">Общее количество использованных токенов</param>
public readonly record struct ResponseUsage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("precached_prompt_tokens")] int PrecachedPromptTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens
);