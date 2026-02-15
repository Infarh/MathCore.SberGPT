using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Запрос на проверку текста.</summary>
internal readonly record struct CheckTextToAIGenerationRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] string Text
    );