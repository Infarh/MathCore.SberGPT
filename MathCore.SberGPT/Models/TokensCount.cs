namespace MathCore.SberGPT.Models;

/// <summary>Информация о количестве токенов для указанной строки ввода</summary>
/// <param name="Input">Строка ввода</param>
/// <param name="Tokens">Количество токенов</param>
/// <param name="Characters">Количество символов</param>
public readonly record struct TokensCount(string Input, int Tokens, int Characters);