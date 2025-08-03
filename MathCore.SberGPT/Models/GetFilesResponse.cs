using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

internal record struct GetFilesResponse([property: JsonPropertyName("data")] FileDescription[] Files);