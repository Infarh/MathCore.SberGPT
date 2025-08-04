using MathCore.SberGPT.Models;

namespace MathCore.SberGPT.Extensions;

public static class StreamingResponseEx
{
    public static IAsyncEnumerable<string> EnumMessagesAsync(this IAsyncEnumerable<StreamingResponse> responses) =>
        responses.Select(response => response.Message);
}
