using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Ответ на проверку текста.</summary>
public readonly record struct CheckTextToAIGenerationResponse(
    [property: JsonPropertyName("category")] CheckTextToAIGenerationResponse.TextClass Test
    , [property: JsonPropertyName("characters")] int TextLength
    , [property: JsonPropertyName("tokens")] int Tokens
    , [property: JsonPropertyName("ai_intervals")] int[][] Intervals
)
{
    /// <summary>Категория текста, определённая моделью детекции.</summary>
    public enum TextClass
    {
        /// <summary>Текст сгенерирован ИИ.</summary>
        AI,
        /// <summary>Текст написан человеком.</summary>
        Human,
        /// <summary>Текст содержит признаки как ИИ, так и человека.</summary>
        Mixed
    }
}