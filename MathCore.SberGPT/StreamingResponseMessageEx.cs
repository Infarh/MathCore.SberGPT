using static MathCore.SberGPT.GptClient;

namespace MathCore.SberGPT;
public static class StreamingResponseMessageEx
{
    public static async Task PrintToAsync(this IAsyncEnumerable<StreamingResponseMessage> responses, TextWriter writer, CancellationToken Cancel = default)
    {
        await foreach (var response in responses.WithCancellation(Cancel).ConfigureAwait(false))
            await writer.WriteAsync(response.Message.AsMemory(), Cancel).ConfigureAwait(false);
    }
}
