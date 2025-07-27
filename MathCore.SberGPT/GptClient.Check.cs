using System.Net.Http.Json;
using System.Text.Json.Serialization;

using static MathCore.SberGPT.GptClient.CheckTextToAIGenerationResponse;

namespace MathCore.SberGPT;

/// <summary>Клиент для проверки текста на генерацию ИИ.</summary>
public partial class GptClient
{
    /// <summary>Проверяет предоставленный текст на предмет генерации ИИ с использованием указанной модели детекции.</summary>
    /// <remarks>Асинхронно анализирует входной текст и определяет, был ли он сгенерирован ИИ. Модель детекции можно указать через параметр <paramref name="Model"/>.</remarks>
    /// <param name="Text">Текст для анализа на предмет генерации ИИ. Не может быть null или пустым.</param>
    /// <param name="Model">Имя используемой модели детекции.<br/>- По умолчанию <see langword="GigaCheckDetection"/> (ai/ai+human/human).<br/>- Альтернатива <see langword="GigaCheckClassification"/>(ai/human).</param>
    /// <param name="Cancel">Токен отмены <see cref="CancellationToken"/> для ожидания завершения операции.</param>
    /// <returns>Задача <see cref="Task"/>, представляющая асинхронную операцию.</returns>
    public async Task<CheckTextToAIGenerationResponse> CheckTextToAIGenerationAsync(string Text, string Model = "GigaCheckDetection", CancellationToken Cancel = default)
    {
        const string url = "ai/check";

        var request = new CheckTextToAIGenerationRequest(Text, Model);

        var response = await Http.PostAsJsonAsync(url, request, __DefaultOptions, Cancel).ConfigureAwait(false);

        var result = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<CheckTextToAIGenerationResponse>(__DefaultOptions, Cancel)
            .ConfigureAwait(false);

        return result;
    }

    /// <summary>Запрос на проверку текста.</summary>
    public readonly record struct CheckTextToAIGenerationRequest(
        [property: JsonPropertyName("input")] string Test,
        [property: JsonPropertyName("model")] string Model
    );

    /// <summary>Ответ на проверку текста.</summary>
    public readonly record struct CheckTextToAIGenerationResponse(
        [property: JsonPropertyName("category")] Class Test
        , [property: JsonPropertyName("characters")] int TextLength
        , [property: JsonPropertyName("tokens")] int Tokens
        , [property: JsonPropertyName("ai_intervals")] int[][] Intervals
    )
    {
        /// <summary>Категория текста, определённая моделью детекции.</summary>
        public enum Class
        {
            /// <summary>Текст сгенерирован ИИ.</summary>
            AI,
            /// <summary>Текст написан человеком.</summary>
            Human,
            /// <summary>Текст содержит признаки как ИИ, так и человека.</summary>
            Mixed
        }
    }
}
