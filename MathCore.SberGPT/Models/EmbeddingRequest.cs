using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

internal readonly record struct EmbeddingRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] IEnumerable<string> Input
);