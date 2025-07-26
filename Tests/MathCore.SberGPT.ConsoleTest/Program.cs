using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using MathCore.SberGPT;
using MathCore.SberGPT.Attributes;
using MathCore.SberGPT.ConsoleTest.HostedServices;
using MathCore.SberGPT.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var json_opt = new JsonSerializerOptions()
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin),
};

string GetJsonScheme(Delegate function)
{
    var scheme = function.GetJsonScheme();
    return scheme.ToJsonString(json_opt);
}

var f1 = GetJsonScheme(Functions.GetWeather);
var f2 = GetJsonScheme(Functions.GetTripDistance);
var f3 = GetJsonScheme(Functions.SendSMS);
var f4 = GetJsonScheme(Functions.SearchMovies);

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

internal static class Functions
{
    #region weather_forecast

    [GPT("weather_forecast", "Возвращает температуру на заданный период")]
    [FunctionPromptExample("Какая погода в Москве в ближайшие три дня", "location:Moscow, Russia", "format:celsius", "num_days:5")]
    public static WeatherForecast? GetWeather(
    [GPT("location", "Местоположение, например, название города")] string Location,
    [GPT("format", "Единицы измерения температуры")] TemperatureUnit? Unit,
    [GPT("num_days", "Период, для которого нужно вернуть")] int NumOfDays)
    => new()
    {
        Location = Location,
        Temperature = 25,
        Forecast = ["Ясно", "Слабый ветер", "Осадков не ожидается", "Метеоритный дождь"],
        Error = null,
    };

    internal readonly record struct WeatherForecast
    {
        [JsonPropertyName("location"), Description("Местоположение, например, название города")]
        public string? Location { get; init; }

        [JsonPropertyName("temperature"), Description("Температура для заданного местоположения")]
        public double? Temperature { get; init; }

        [JsonPropertyName("forecast"), Description("Описание погодных условий")]
        public string[]? Forecast { get; init; }

        [JsonPropertyName("Error"), Description("Возвращается при возникновении ошибки. Содержит описание ошибки")]
        public string? Error { get; init; }
    }

    internal enum TemperatureUnit
    {
        [GPT("celsius")] Celsius,
        [GPT("fahrenheit")] Fahrenheit
    }

    #endregion

    #region calculate_trip_distance

    [GPT("calculate_trip_distance", "Рассчитать расстояние между двумя местоположениями")]
    [FunctionPromptExample("Насколько далеко от Москвы до Санкт-Петербурга?", "start_location:Москва", "end_location:Санкт-Петербург")]
    public static TripDistance GetTripDistance(
        [GPT("start_location", "Начальное местоположение")] string StartLocation,
        [GPT("end_location", "Конечное местоположение")] string EndLocation
        )
        => new(10);

    public readonly record struct TripDistance(
        [property: JsonPropertyName("distance"), Description("Расстояние между начальным и конечным местоположением в километрах")] int Distance);

    #endregion

    #region send_sms

    [GPT("send_sms", "Отправить SMS-сообщение")]
    [FunctionPromptExample("Можешь ли ты отправить SMS-сообщение на номер 123456789 с содержимым 'Привет, как дела?'",
        "recipient:123456789", "message:Привет, как дела?")]
    public static SendSMSResult SendSMS(
        [GPT("recipient", "Номер телефона получателя")] string Recipient,
        [GPT("message", "Содержимое сообщения")] string Message)
        => new("Sent", "Сообщение успешно отправлено");

    public readonly record struct SendSMSResult(
        [property: JsonPropertyName("status"), Description("Статус отправки сообщения")] string Status,
        [property: JsonPropertyName("message"), Description("Сообщение о результате отправки SMS")] string Message);

    #endregion

    #region search_movies

    [GPT("search_movies", "Поиск фильмов на основе заданных критериев")]
    [FunctionPromptExample("Найди все фильмы жанра комедия", "genre:комедия")]
    public static MovieSearchResult SearchMovies(
        [GPT("genre", "Жанр фильма")] string? Genre,
        [GPT("year", "Год выпуска фильма")] int? Year,
        [GPT("actor", "Имя актера, снимавшегося в фильме")] string? Actor)
        => new("В джазе только девушки", "Операция Ы", "Бриллиантовая рука", "Кавказская пленница", "12 стульев");

    public readonly record struct MovieSearchResult(
        [property: JsonPropertyName("movies"), Description("Список названий фильмов, соответствующих заданным критериям поиска")]
        [property: ItemsDescription("Название фильма")]
        params string[] Movies);

    #endregion
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