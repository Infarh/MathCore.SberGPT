using System.Collections;
using System.Text;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о количестве токенов и символов для набора входных строк</summary>
/// <param name="Counts">Список информации о количестве токенов и символов для каждой строки</param>
public readonly record struct TokensCountInfo(IReadOnlyList<TokensCount> Counts) : IReadOnlyList<TokensCount>
{
    /// <summary>Неявное преобразование массива в TokensCountInfo</summary>
    public static implicit operator TokensCountInfo(TokensCount[] list) => new(list);

    /// <summary>Общее количество токенов для всех строк</summary>
    [JsonIgnore] public int Tokens => Counts.Sum(c => c.Tokens);
    /// <summary>Общее количество символов для всех строк</summary>
    [JsonIgnore] public int Characters => Counts.Sum(c => c.Characters);

    /// <summary>Объединённый текст всех входных строк</summary>
    [JsonIgnore] public string Input => Counts.Aggregate(new StringBuilder(), (s, c) => s.AppendLine(c.Input), s => s.Length == 0 ? string.Empty : s.ToString(0, s.Length - Environment.NewLine.Length));

    /// <inheritdoc/>
    IEnumerator<TokensCount> IEnumerable<TokensCount>.GetEnumerator() => Counts.GetEnumerator();
    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Counts).GetEnumerator();
    /// <inheritdoc/>
    int IReadOnlyCollection<TokensCount>.Count => Counts.Count;
    /// <inheritdoc/>
    public TokensCount this[int index] => Counts[index];
}