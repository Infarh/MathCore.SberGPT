using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о количестве токенов</summary>
/// <param name="Tokens">Количество токенов</param>
/// <param name="Characters">Количество символов</param>
internal readonly record struct TokensCountResponse(
    [property: JsonPropertyName("tokens")] int Tokens,
    [property: JsonPropertyName("characters")] int Characters);