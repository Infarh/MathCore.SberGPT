using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация об ошибке или предупреждении</summary>
public record ValidationFunctionResultInfo(
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("schema_location")] string SchemaLocation
);