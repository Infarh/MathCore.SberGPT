using MathCore.SberGPT;
using MathCore.SberGPT.Attributes;
using MathCore.SberGPT.ConsoleTest.HostedServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder();

var cfg = builder.Configuration;
cfg.AddJsonFile("appsettings.json", true);
cfg.AddUserSecrets(typeof(Program).Assembly);

var log = builder.Logging;
log.AddConsole();
log.AddDebug();
log.AddFilter((n, l) => (n, l) switch
{
    ("MathCore.SberGPT.GptClient", >= LogLevel.Trace) => true,
    (_, >= LogLevel.Information) => true,
    _ => false
});
log.AddConfiguration(cfg);

var srv = builder.Services;
srv.AddSberGPT();

srv.AddHostedService<MainWorker>();

var app = builder.Build();

await app.RunAsync();

Console.WriteLine("End.");
return;

[FunctionName("GetCityWeather")]
[FunctionDescription("Получение погоды в указанном городе в заданных единицах измерения")]
static string GetWeather(
    [FunctionDescription("Город")] string City,
    [FunctionDescription("Единицы измерения температуры")] string Unit)
    => $"Погода в {City}: солнечно, 25 deg {Unit}";

internal class StudentGroup
{
    public int Id { get; set; }

    public string Name { get; set; }
}

internal class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public StudentGroup Group { get; set; } = null!;

    public double Rating { get; set; }

    public Dictionary<string, string> MetaData { get; set; } = [];
}

//var builder = Host.CreateDefaultBuilder(args)
//        .ConfigureAppConfiguration(c => c.AddUserSecrets(typeof(Program).Assembly))
//        .ConfigureServices(s => s
//            .AddSberGPT()
//        //.AddHostedService<TestWorker>()
//        )
//    ;

//using var app = builder.Build();

//await using (var scope = app.Services.CreateAsyncScope())
//{
//    var client = scope.ServiceProvider.GetRequiredService<GptClient>();

//    var file = new FileInfo("diplom.docx");

//    var text = Word.File(file)
//        .SkipWhile(l => !string.Equals(l, "введение", StringComparison.OrdinalIgnoreCase))
//        .Select(l => l.Trim())
//        .WhereNot(string.IsNullOrWhiteSpace)
//        .ToArray();

//    var response = await client.GetTokensCountAsync(text);

//    Console.WriteLine();
//}

////await app.RunAsync();

//Console.WriteLine("End.");

//return;

//class TestWorker(GptClient gpt, ILogger<TestWorker> log) : IHostedService, IDisposable
//{
//    private readonly CancellationTokenSource _Cancellation = new();
//    private Task _Task = null!;

//    private async Task WorkTask(CancellationToken Cancel)
//    {
//        var models = await gpt.GetModelsAsync(Cancel).ConfigureAwait(false);

//        var tokens = await gpt.GetTokensCountAsync([
//            "Я к вам пишу — чего же боле?",
//            "Что я могу еще сказать?",
//            "Теперь, я знаю, в вашей воле",
//            "Меня презреньем наказать.",
//            "Но вы, к моей несчастной доле",
//            "Хоть каплю жалости храня,",
//            "Вы не оставите меня."],
//            Cancel: Cancel).ConfigureAwait(false);

//        //foreach(var (input, tokens_count, count) in tokens)
//        //    log.LogInformation("{input} токенов: {tokens}, chars: {count}", input, tokens_count, count);

//        //var response = await gpt.RequestAsync(
//        //    [
//        //        new("Ты профессиональный синоптик. Дай точный прогноз погоды используя простой язык.", RequestRole.system),
//        //        new("Какая погода в Москве сегодня?")
//        //    ], Cancel: Cancel)
//        //    .ConfigureAwait(false);

//        //var image_guid = await gpt.GenerateImageAsync(
//        //    [
//        //        new("Ты художник со стажем. Нарисуй изображение в стиле акварели.", RequestRole.system),
//        //        new("Нарисуй прохожих на тёмной улице в свете фонарей. На тротуаре лужи.")
//        //    ], 
//        //    Cancel: Cancel)
//        //    .ConfigureAwait(false);

//        //var image_bytes = await gpt.DownloadImageById(image_guid, Cancel).ConfigureAwait(false);

//        //await File.WriteAllBytesAsync("img4.jpg", image_bytes, Cancel).ConfigureAwait(false);

//        await gpt.GenerateAndDownloadImageAsync(
//                [
//                    new("Ты художник со стажем. Используй фотореалистичный стиль.", RequestRole.system),
//                    new("Нарисуй порт в городе Выборг")
//                ],
//                (bytes, cancel) => File.WriteAllBytesAsync("Viborg.jpg", bytes, cancel),
//                Cancel: Cancel)
//            .ConfigureAwait(false);
//    }

//    Task IHostedService.StartAsync(CancellationToken cancel)
//    {
//        _Task = Task.Run(async () => await WorkTask(_Cancellation.Token), cancel);
//        return Task.CompletedTask;
//    }

//    async Task IHostedService.StopAsync(CancellationToken cancel)
//    {
//        await _Cancellation.CancelAsync().ConfigureAwait(false);
//        try
//        {
//            await _Task.ConfigureAwait(false);
//        }
//        catch (OperationCanceledException)
//        {

//        }
//    }

//    public void Dispose() => _Cancellation.Dispose();
//}