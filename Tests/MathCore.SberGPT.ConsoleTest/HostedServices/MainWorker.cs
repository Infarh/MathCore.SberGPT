using Microsoft.Extensions.Hosting;

namespace MathCore.SberGPT.ConsoleTest.HostedServices;

internal class MainWorker(GptClient gpt) : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _Cancellation = new();
    private Task _WorkingTask = null!;

    async Task IHostedService.StopAsync(CancellationToken Cancel) => await _Cancellation.CancelAsync().ConfigureAwait(false);

    Task IHostedService.StartAsync(CancellationToken Cancel)
    {
        Start();
        return Task.CompletedTask;
    }

    private void Start()
    {
        var cancel = _Cancellation.Token;
        _WorkingTask = StartAsync(cancel);
    }

    private async Task StartAsync(CancellationToken Cancel)
    {
        await Task.Yield().ConfigureAwait(false);

        var tokens = await gpt.GetTokensBalanceAsync(Cancel).ConfigureAwait(false);

        var response = await gpt.RequestAsync("Как твои дела?", Cancel: Cancel).ConfigureAwait(false);
    }

    void IDisposable.Dispose() => _Cancellation.Dispose();
}
