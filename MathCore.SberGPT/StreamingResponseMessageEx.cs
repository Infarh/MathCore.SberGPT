using MathCore.SberGPT.Models;

namespace MathCore.SberGPT;

/// <summary>Методы-расширения для работы с потоковыми сообщениями ответа модели</summary>
public static class StreamingResponseMessageEx
{
    /// <summary>Асинхронно выводит объединённые сообщения из последовательности потоковых ответов в указанный <see cref="TextWriter"/></summary>
    /// <param name="responses">Асинхронная последовательность потоковых сообщений</param>
    /// <param name="writer">Писатель для вывода сообщений</param>
    /// <param name="Cancel">Токен отмены</param>
    public static async Task PrintToAsync(this IAsyncEnumerable<StreamingResponseMsg> responses, TextWriter writer, CancellationToken Cancel = default)
    {
        await foreach (var response in responses.WithCancellation(Cancel).ConfigureAwait(false))
            await writer.WriteAsync(response.Message.AsMemory(), Cancel).ConfigureAwait(false);
    }
}
