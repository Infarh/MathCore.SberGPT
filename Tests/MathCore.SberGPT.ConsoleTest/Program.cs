using MathCore.SberGPT;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


Console.WriteLine("Start.");
Console.WriteLine("");

var builder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(c => c.AddUserSecrets(typeof(Program).Assembly))
        .ConfigureServices(s => s
            .AddSberGPT()
            .AddHostedService<TestWorker>()
        )
    ;

using var app = builder.Build();

var app_config = app.Services.GetRequiredService<IConfiguration>();

var t = app_config["test"];

await app.RunAsync();

Console.WriteLine("End.");

class TestWorker(GptClient gpt, ILogger<TestWorker> log) : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _Cancellation = new();
    private Task _Task = null!;

    private async Task WorkTask(CancellationToken Cancel)
    {
        var models = await gpt.GetModelsAsync(Cancel).ConfigureAwait(false);

        //var tokens = await gpt.GetTokensCountAsync([
        //    "Я к вам пишу — чего же боле?", 
        //    "Что я могу еще сказать?",
        //    "Теперь, я знаю, в вашей воле",
        //    "Меня презреньем наказать.",
        //    "Но вы, к моей несчастной доле",
        //    "Хоть каплю жалости храня,",
        //    "Вы не оставите меня."], 
        //    Cancel: Cancel).ConfigureAwait(false);

        //foreach(var (input, tokens_count, count) in tokens)
        //    log.LogInformation("{input} токенов: {tokens}, chars: {count}", input, tokens_count, count);

        var response = await gpt.RequestAsync(
            [
                new("Ты профессиональный синоптик. Дай точный прогноз погоды используя простой язык.", RequestRole.system),
                new("Какая погода в Москве сегодня?")
            ], Cancel: Cancel)
            .ConfigureAwait(false);
    }

    Task IHostedService.StartAsync(CancellationToken cancel)
    {
        _Task = Task.Run(async () => await WorkTask(_Cancellation.Token), cancel);
        return Task.CompletedTask;
    }

    async Task IHostedService.StopAsync(CancellationToken cancel)
    {
        await _Cancellation.CancelAsync().ConfigureAwait(false);
        try
        {
            await _Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {

        }
    }

    public void Dispose() => _Cancellation.Dispose();
}