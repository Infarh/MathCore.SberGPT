using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о модели сервиса</summary>
/// <param name="ModelId">Идентификатор модели</param>
internal readonly record struct ModelInfo([property: JsonPropertyName("id")] string ModelId);