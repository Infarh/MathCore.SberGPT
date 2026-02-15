using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Результат валидации функции</summary>
public record ValidationFunctionResult(
    [property: JsonPropertyName("message")] string Message
    , [property: JsonPropertyName("json_ai_rules_version")] string AIRulesVersion
    , [property: JsonPropertyName("errors")] IReadOnlyList<ValidationFunctionResultInfo>? Errors
    , [property: JsonPropertyName("warnings")] IReadOnlyList<ValidationFunctionResultInfo>? Warnings
    )
{
    /// <summary>Признак наличия ошибок валидации</summary>
    public bool HasErrors => Errors?.Any() == true;

    /// <summary>Признак наличия предупреждений валидации</summary>
    public bool HasWarnings => Warnings?.Any() == true;

    /// <summary>Признак успешной валидации (отсутствие ошибок)</summary>
    public bool IsCorrect => !HasErrors;
}