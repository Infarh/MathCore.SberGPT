using MathCore.SberGPT;
using MathCore.SberGPT.Attributes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

const string token = "eyJjdHk-------3QiLCJlbmMiOiJBM--------------iYWxnIjoiUlNBLU9B---------In0.uCM4bgxJO-------------Fn8sqpx3d52qJ_SecYxdCuROwk_q4EV-----1hp4ECTUV_cTEjBYmtTxVTl6cpcKjC8-z-ukWOa-------sxLQ-Cb39_iApVpOvjsjaxnwR_GLG2fAGBcbn57kgUCdKMnvNmvLEOH0s25pH0Bd3jhdA5DtU6GUM1TgDXLXFokXlQJPSX4lxpD50OtMpjk-umwV3M7Hlm4Cmwgts9RAtqmK_zdSOsFOgmk9NgFAlRnR14nlqB3eBW7g62JVzcmLCuzy_HPy7CxDIwQWcib5vynrO1RwuMwue91o5QpZn7Q.ibb1LIdLko4Yfn5uQJv6Xg.ORM-2zGX6SNKMEDswOrjskzMEe2vbQR90PdfOzciyhGRSsYChZxDSJZe67tE1gsP2bn5DqgsuDhdjppXgQ6R-lz26iUl8_LG8IZRLsW_s7FZlQEoC15jeu2j9WwoS_-v8sSZFMCKwPQCKbOnB31qsnmIcbtYRQQr15y8mMZaRoO616-BfdSnpiGdDDjpUJkgFTmn0uVavxjcKsL6hYGYNxZ_rQZoXO0T09TRMNoNYb07lrcIidEFjLiNiP0RH7SwdUUQa2x0fd6mMl09H7BWwTS5h3c9H0lWn_vkliwPwtzBJ6mYZ7iv8Df1TXDoPsyoL-rahputZUSezqorHg-vEERn8VXdtKuAazG6qUFXP8QGx6wjCWG2xgH9Wbh64F3flZZQWOS1bya2xEu1K8-x3GMyoDKxHHEIAL4X71qcFT1Je179Vzc9WT7YRKDFqwTMzWTQdfVjLGRNSUp8WLcTePY78TIQA5ccmtuLOMWchpHHeydq4vsJ5LaY4zfSZp7Gqj7XzsFDf9MTttx-yvGHNmopW0ofFGUfJht8PVwfBg9TWENqIfCXwFKglDVPvOH85nO92i0V-mNiM2q9MCbOXAQQKYTLSmhIUCVo-AweinOeQj_G9ufsAC0et4MHno4HUNVhrCYRZajQWKONlrV9DvqfBSylXdFa0R00mbaT1ZcnznxSQMwzGIzuBBCSon_taxFvgQwphEu8P5c_unzVoi4Xwt6v0xfHtsfW4OhurUY.ADO0gk--------JAvpkexSy9t6yTtUw";
const string data_path = "d:\\TestFile.txt";

using (var client = new HttpClient())
{
    var request = new HttpRequestMessage(HttpMethod.Post, "https://gigachat.devices.sberbank.ru/api/v1/files");

    request.Headers.Accept.ParseAdd("*/*");
    request.Headers.Authorization = new("Bearer", token);
    request.Headers.UserAgent.ParseAdd("MathCore.SberGPT/1.0");

    var content = new MultipartFormDataContent
    {
    };
    content.Add(new StreamContent(File.OpenRead(data_path)), "file", "file.txt");
    request.Content = content;

    try
    {
        using var response = await client.SendAsync(request);
        var response_body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(response_body);
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine(e.Message);
    }
}

if (false)
    using (var http = new HttpClient())
    {
        http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MathCore.SberGPT/1.0");

        using var form = new MultipartFormDataContent
    {
        { new StreamContent(File.OpenRead(data_path)), "file", "TestFile.txt" },
        { new StringContent("general"), "purpose" }
    };

        var response = await http.PostAsync("https://gigachat.devices.sberbank.ru/api/v1/", form);

        var str = await response.Content.ReadAsStringAsync();
        Console.WriteLine(str);
    }

var cfg = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true)
    .AddUserSecrets(typeof(Program).Assembly)
    .Build();

var log = LoggerFactory
    .Create(b => b
        .AddConsole()
        .AddDebug()
        .AddFilter((n, l) => (n, l) switch
        {
            ("MathCore.SberGPT.GptClient", >= LogLevel.Trace) => true,
            (_, >= LogLevel.Information) => true,
            _ => false
        })
        .AddConfiguration(cfg)
        )
    .CreateLogger<GptClient>();

var cfg_sber = cfg.GetSection("sber");

var gpt = new GptClient(cfg_sber, log);

var files = await gpt.GetFilesAsync();


using (var data = new MemoryStream())
{
    var writer = new StreamWriter(data);
    writer.WriteLine("Hello World!");
    writer.Flush();
    data.Seek(0, SeekOrigin.Begin);

    await gpt.UploadFileAsync($"hello.txt", data);
}

var files2 = await gpt.GetFilesAsync();


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