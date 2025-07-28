# MathCore.SberGPT

Клиент для работы с языковой моделью GigaChat от Сбербанка на платформе .NET 9.

## Описание

MathCore.SberGPT - это библиотека для .NET, предоставляющая удобный интерфейс для взаимодействия с API GigaChat от Сбербанка. Библиотека поддерживает:

- Отправку текстовых запросов к модели
- Потоковую передачу ответов
- Работу с историей чата
- Вызов пользовательских функций (function calling)
- Генерацию и загрузку изображений
- Векторизацию текста (embeddings)
- Подсчет токенов

## Установка

```bash
dotnet add package MathCore.SberGPT
```

## Настройка

Для работы с библиотекой необходимо получить токен доступа к API GigaChat и добавить его в конфигурацию приложения.

### Конфигурация через appsettings.json

appsettings.json:
```json
  "Sber": {
    "Scope": "GIGACHAT_API_PERS",
    "UserAgent": "MathCore.SberGPT/1.0",
  } 
```

secrets.json:
```json
  "sber": {
    "ClientId": "XXXXXXXX-YYYY-ZZZZ-AAAA-BBBBBBBBBBBB",
    "secret": "ваш_секретный_ключ",
    "TokenEncryptionKey": "Giga.Chat-Password",
    "TokenStoreFile": "chat-gpt.token"
  } 
```

Параметры конфигурации:
- `Scope`: определяет область доступа к API. Доступны следующие значения:
  - `GIGACHAT_API_PERS` - для физических лиц
  - `GIGACHAT_API_B2B` - для ИП и юридических лиц
  - `GIGACHAT_API_CORP` - для корпоративных клиентов
- `UserAgent`: строка User-Agent для HTTP-запросов
- `ClientId`: уникальный идентификатор клиента, полученный при регистрации приложения в [Сбербанке](https://developers.sber.ru/studio/workspaces)
- `TokenEncryptionKey`: ключ для шифрования токенов авторизации, получаемых от сервера в файле кеша
- `TokenStoreFile`: имя файла для хранения кеша токенов авторизации

Если не указано имя файла кеша токенов, либо не задан пароль, то токены будут кешироваться на диске. Будет работать только кеш токена в памяти клиента. При перезапуске приложения токен будет запрошен у сервера заново.

Время жизни токена, выдаваемого сервером не текущий момент состоавляет 30 минут.


### Конфигурация через User Secrets

```bash
dotnet user-secrets set "secret" "ваш_секретный_ключ"
dotnet user-secrets set "TokenEncryptionKey" "Giga.Chat-Password"
dotnet user-secrets set "TokenStoreFile" "chat-gpt.token"
```

## Примеры использования

### Пример 0. Простейший пример использования

```csharp
var cfg = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Sber:ClientId"] = "258===e8-e==8-4==8-b==e-7===bda===4e",
        ["Sber:secret"] = "MjU---MxZ---ZTk---00N---LWJ---UtN---M2J---Y5M---OmJ---QxZ---LTM---ItN---Ni0---RhL---OTc---RhM---Ywi3",
    })
    .Build();

var gpt = new GptClient(cfg.GetSection("Sber"));

await gpt.AddFunctionAsync(Functions.GetWeather);

var response = await gpt.RequestAsync("Можно ли будет завтра пойти искупаться в Самаре?");

Console.WriteLine(response);
```
Параметры:
- ClientId - идентификатор клиента
- secret - секретный ключ, полученный при регистрации приложения

получить [здесь](https://developers.sber.ru/studio/workspaces)

### Пример 1. Простой текстовый запрос

```csharp
using MathCore.SberGPT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Создание конфигурации
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();

// Создание логгера
using var logger_factory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = logger_factory.CreateLogger<GptClient>();

// Создание клиента
var client = new GptClient(config, logger);

// Отправка запроса
var response = await client.RequestAsync("Привет! Как дела?");

// Вывод результата
Console.WriteLine(response.AssistMessage);
```

### Пример 2. Консольное приложение с диалоговым режимом

```csharp
using MathCore.SberGPT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<GptClient>();

var client = new GptClient(config, logger);

// Установка системного промпта
client.SystemPrompt = "Ты дружелюбный помощник. Отвечай кратко и по делу.";

Console.WriteLine("Добро пожаловать в диалог с GigaChat! Введите 'выход' для завершения.");

while (true)
{
    Console.Write("Вы: ");
    var user_input = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(user_input) || user_input.ToLower() == "выход")
        break;
    
    try
    {
        // Отправка запроса с сохранением истории
        var response = await client.RequestAsync(user_input);
        
        Console.WriteLine($"GigaChat: {response.AssistMessage}");
        Console.WriteLine($"(Использовано токенов: {response.Usage.TotalTokens})");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

Console.WriteLine("До свидания!");
```

### Пример 3. Использование функций обратного вызова с запросом о погоде

```csharp
using System.ComponentModel;
using System.Text.Json.Serialization;
using MathCore.SberGPT;
using MathCore.SberGPT.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

using var logger_factory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = logger_factory.CreateLogger<GptClient>();

var client = new GptClient(config, logger);

// Добавление функции для получения информации о погоде
await client.AddFunctionAsync(GetWeather);

// Запрос к модели с использованием функции
var response = await client.RequestAsync("Какая погода сейчас в Москве?");

Console.WriteLine(response.AssistMessage);

// Определение функции для получения погоды
[GPT("get_weather", "Получает информацию о текущей погоде в указанном городе")]
static WeatherInfo GetWeather(
    [GPT("city", "Название города для получения прогноза погоды")] string City,
    [GPT("units", "Единицы измерения температуры")] TemperatureUnit Units = TemperatureUnit.Celsius)
{
    // В реальном приложении здесь был бы вызов API погоды
    return new WeatherInfo
    {
        City = City,
        Temperature = Units == TemperatureUnit.Celsius ? 15 : 59,
        Description = "Переменная облачность",
        Humidity = 65,
        WindSpeed = 12
    };
}

// Структуры для работы с функцией погоды
public record WeatherInfo
{
    [JsonPropertyName("city"), Description("Название города")]
    public string City { get; init; } = "";
    
    [JsonPropertyName("temperature"), Description("Температура")]
    public int Temperature { get; init; }
    
    [JsonPropertyName("description"), Description("Описание погодных условий")]
    public string Description { get; init; } = "";
    
    [JsonPropertyName("humidity"), Description("Влажность в процентах")]
    public int Humidity { get; init; }
    
    [JsonPropertyName("wind_speed"), Description("Скорость ветра в км/ч")]
    public int WindSpeed { get; init; }
}

public enum TemperatureUnit
{
    [GPT("celsius")] Celsius,
    [GPT("fahrenheit")] Fahrenheit
}
```

## Дополнительные возможности

### Генерация изображений

```csharp
// Генерация изображения
var image_guid = await client.GenerateImageAsync([
    new("Ты талантливый художник", RequestRole.system),
    new("Нарисуй красивый закат над морем")
]);

// Загрузка изображения
var image_bytes = await client.DownloadImageById(image_guid);
await File.WriteAllBytesAsync("sunset.jpg", image_bytes);
```

### Потоковая передача ответов

```csharp
await foreach (var chunk in client.RequestStreamingAsync("Расскажи интересную историю"))
{
    Console.Write(chunk.MessageAssistant);
}
```

### Подсчет токенов

```csharp
var token_info = await client.GetTokensCountAsync("Пример текста для подсчета токенов");
Console.WriteLine($"Токенов: {token_info.Tokens}, Символов: {token_info.Characters}");
```

## Конфигурация с Dependency Injection

```csharp
using MathCore.SberGPT.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

// Добавление клиента GigaChat в DI контейнер
builder.Services.AddSberGPT();

var app = builder.Build();

// Использование клиента через DI
var gpt_client = app.Services.GetRequiredService<GptClient>();
var response = await gpt_client.RequestAsync("Привет!");
```

## Лицензия

MIT License