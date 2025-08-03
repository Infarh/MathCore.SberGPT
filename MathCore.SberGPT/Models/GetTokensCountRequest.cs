using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Запрос о количестве токенов</summary>
/// <param name="Model">Тип модели</param>
/// <param name="Input">Ввод пользователя, для которого надо рассчитать количество токенов</param>
internal readonly record struct GetTokensCountRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] string[] Input);