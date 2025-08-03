using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о количестве токенов</summary>
/// <param name="Tokens">Перечень значений количеств оставшихся токенов по моделям</param>
public readonly record struct BalanceInfo(
    [property: JsonPropertyName("balance")] BalanceInfoValue[] Tokens
    )
{
    public override string ToString() => string.Join("; ", Tokens);
}