using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о количестве токенов модели</summary>
/// <param name="Model">Название модели</param>
/// <param name="TokensElapsed">Количество токенов</param>
public readonly record struct BalanceInfoValue(
    [property: JsonPropertyName("usage")] string Model,
    [property: JsonPropertyName("value")] int TokensElapsed
)
{
    public override string ToString() => $"{Model}:{TokensElapsed}";
}