using System.Net.Http.Json;
using System.Text.Json;

namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class HttpResponseMessageEx
{
    public static async ValueTask<T> AsJsonAsync<T>(this HttpResponseMessage response, CancellationToken Cancel = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        var result = await response
            .EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<T>(cancellationToken: Cancel)
            .ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("Ошибка получения результата запроса");
    }

    public static async ValueTask<T> AsJsonAsync<T>(
        this HttpResponseMessage response,
        JsonSerializerOptions? JsonOptions,
        CancellationToken Cancel = default)
    {
        var result = await response
            .EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<T>(JsonOptions, Cancel)
            .ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("Ошибка получения результата запроса");
    }

    public static Task<Stream> AsStream(this HttpResponseMessage response, CancellationToken Cancel = default) =>
        response
            .EnsureSuccessStatusCode()
            .Content.ReadAsStreamAsync(Cancel);
}
