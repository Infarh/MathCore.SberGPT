using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о вызове функции</summary>
/// <param name="Name">Название функции</param>
/// <param name="Arguments">Перечень аргументов функции с их значениями</param>
public readonly record struct ResponseChoiceMsgFunc(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] JsonObject Arguments
);