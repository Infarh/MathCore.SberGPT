using MathCore.SberGPT.Models;

namespace MathCore.SberGPT.Extensions;

/// <summary>Методы расширения для StreamingResponse</summary>
public static class StreamingResponseEx
{
    /// <summary>Перечисляет сообщения из потока ответов</summary>
    /// <param name="responses">Поток ответов</param>
    /// <returns>Асинхронное перечисление сообщений</returns>
    public static IAsyncEnumerable<string> EnumMessagesAsync(this IAsyncEnumerable<StreamingResponse> responses) =>
        responses.Select(response => response.Message);
}
