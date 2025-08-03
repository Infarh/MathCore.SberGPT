using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Запрос на проверку текста.</summary>
public readonly record struct CheckTextToAIGenerationRequest(
    [property: JsonPropertyName("input")] string Test,
    [property: JsonPropertyName("model")] string Model
);