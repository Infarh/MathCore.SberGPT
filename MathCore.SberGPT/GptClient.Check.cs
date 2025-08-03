using System.Net.Http.Json;

using MathCore.SberGPT.Models;

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

        var response = await Http.PostAsJsonAsync(url, request, JsonOptions, Cancel).ConfigureAwait(false);

        var result = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<CheckTextToAIGenerationResponse>(JsonOptions, Cancel)
            .ConfigureAwait(false);

        return result;
    }
}
