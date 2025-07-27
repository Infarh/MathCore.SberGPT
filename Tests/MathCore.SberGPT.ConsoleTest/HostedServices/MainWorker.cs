using System.ComponentModel;
using System.Text.Json.Serialization;

using MathCore.Annotations;
using MathCore.SberGPT.Attributes;

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

        //var response = await gpt.RequestAsync("Как твои дела?", Cancel: Cancel).ConfigureAwait(false);

        try
        {
            var info1 = await gpt.AddFunctionAsync(Functions.GetWeather, Cancel);

            var result = await gpt.RequestAsync("Можно ли будет завтра купаться в Москве?", Cancel: Cancel);
            //var result = await gpt.RequestAsync("Можно ли будет купаться в Москве завтра?", Cancel: Cancel);

            var assist_messages = result.AssistMessages.ToArray();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            throw;
        }
    }

    void IDisposable.Dispose() => _Cancellation.Dispose();
}

internal static class Functions
{
    #region weather_forecast

    [GPT("weather_forecast", "Возвращает погоду на заданный период")]
    [PromptExample("Какая погода в Москве в ближайшие три дня?", "location:Moscow, Russia", "format:celsius", "num_days:5")]
    public static WeatherForecast? GetWeather(
    [GPT("location", "Местоположение, например, название города")] string Location,
    [GPT("format", "Единицы измерения температуры")] TemperatureUnit? Unit,
    [GPT("num_days", "Период, для которого нужно вернуть")] int NumOfDays)
    => new()
    {
        Location = Location,
        Temperature = 32,
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
        [UsedImplicitly]
        [GPT("celsius")] Celsius,
        [UsedImplicitly]
        [GPT("fahrenheit")] Fahrenheit
    }

    #endregion

    #region calculate_trip_distance

    [GPT("calculate_trip_distance", "Рассчитать расстояние между двумя местоположениями")]
    [PromptExample("Насколько далеко от Москвы до Санкт-Петербурга?", "start_location:Москва", "end_location:Санкт-Петербург")]
    public static TripDistance GetTripDistance(
        [GPT("start_location", "Начальное местоположение")] string StartLocation,
        [GPT("end_location", "Конечное местоположение")] string EndLocation
        )
        => new(635);

    public readonly record struct TripDistance(
        [property: JsonPropertyName("distance"), Description("Расстояние между начальным и конечным местоположением в километрах")] int Distance);

    #endregion

    #region send_sms

    [GPT("send_sms", "Отправить SMS-сообщение")]
    [PromptExample("Можешь ли ты отправить SMS-сообщение на номер 123456789 с содержимым 'Привет, как дела?'",
        "recipient:123456789", "message:Привет, как дела?")]
    public static SendSMSResult SendSMS(
        [GPT("recipient", "Номер телефона получателя")] string Recipient,
        [GPT("message", "Содержимое сообщения")] string Message)
        => new("Sent", $"Сообщение {Recipient} успешно отправлено");

    public readonly record struct SendSMSResult(
        [property: JsonPropertyName("status"), Description("Статус отправки сообщения")] string Status,
        [property: JsonPropertyName("message"), Description("Сообщение о результате отправки SMS")] string Message);

    #endregion

    #region search_movies

    [GPT("search_movies", "Поиск фильмов на основе заданных критериев")]
    [PromptExample("Найди все фильмы жанра комедия", "genre:комедия")]
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