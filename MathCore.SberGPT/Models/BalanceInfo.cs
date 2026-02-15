using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о количестве токенов</summary>
/// <param name="Tokens">Перечень значений количеств оставшихся токенов по моделям</param>
internal readonly record struct BalanceInfo(
    [property: JsonPropertyName("balance")] IReadOnlyList<BalanceInfo.Value> Tokens
    )
{
    /// <summary>Информация о количестве токенов модели</summary>
    /// <param name="Model">Название модели</param>
    /// <param name="TokensElapsed">Количество токенов</param>
    public readonly record struct Value(
        [property: JsonPropertyName("usage")] string Model,
        [property: JsonPropertyName("value")] int TokensElapsed
    )
    {
        public override string ToString() => $"{Model}:{TokensElapsed}";
    }


    public override string ToString() => string.Join("; ", Tokens);
}