using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Ответ сервера, содержащий список моделей</summary>
/// <param name="Models">Список моделей сервиса</param>
internal readonly record struct ModelsInfosListResponse([property: JsonPropertyName("data")] ModelInfo[] Models);