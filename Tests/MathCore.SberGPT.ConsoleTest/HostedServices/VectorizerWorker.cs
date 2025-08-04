using Microsoft.Extensions.Hosting;

namespace MathCore.SberGPT.ConsoleTest.HostedServices;

internal class VectorizerWorker(GptClient GPT) : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _Cancellation = new();

    private Task _WorkingTask = null!;

    async Task IHostedService.StopAsync(CancellationToken Cancel) => await _Cancellation.CancelAsync().ConfigureAwait(false);

    Task IHostedService.StartAsync(CancellationToken Cancel) => _WorkingTask = WorkingAsync();

    private async Task WorkingAsync()
    {
        await Task.Yield().ConfigureAwait(false);

        const string text =
            """
            Шла Саша по шоссе и сосала сушку.
            """;

        var embeddings = await GPT.GetEmbeddingsAsync([text]);
    }

    public void Dispose()
    {
        _Cancellation.Cancel();
        _Cancellation.Dispose();
    }
}
