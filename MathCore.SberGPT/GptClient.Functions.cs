using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using MathCore.SberGPT.Attributes;
using MathCore.SberGPT.Extensions;

using Microsoft.Extensions.Logging;

using static MathCore.SberGPT.GptClient.ValidationFunctionResult;

namespace MathCore.SberGPT;

public partial class GptClient
{

    public record ValidationFunctionResult(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("json_ai_rules_version")] string AIRulesVersion,
        [property: JsonPropertyName("errors")] IEnumerable<Info>? Errors,
        [property: JsonPropertyName("warnings")] IEnumerable<Info>? Warnings
    )
    {
        public record Info(
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("schema_location")] string SchemaLocation
        );

        public bool HasErrors => Errors?.Any() == true;

        public bool HasWarnings => Warnings?.Any() == true;

        public bool IsCorrect => !HasErrors;
    }

    private static readonly JsonSerializerOptions __ValidateFunctionSerializationOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        WriteIndented = false,
    };

    public async Task<ValidationFunctionResult> ValidateFunctionAsync(Delegate Function, CancellationToken Cancel = default)
    {
        var function_info = Function.GetJsonScheme();

        var function_name = function_info["name"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Не удалось получить имя функции из схемы");

        var str_content = JsonSerializer.Serialize(function_info, __ValidateFunctionSerializationOptions);
        // https://gigachat.devices.sberbank.ru/api/v1/functions/validate
        var request = new HttpRequestMessage(HttpMethod.Post, "functions/validate")
        {
            Content = new StringContent(str_content, null, "application/json") { Headers = { ContentType = new("application/json") } }
        };

        var response = await Http.SendAsync(request, Cancel);
        try
        {
            var result = await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<ValidationFunctionResult>(__DefaultOptions, Cancel)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Ошибка получения данных от сервера");

            if (result.IsCorrect)
                _Log.LogInformation("Функция {function} успешно прошла валидацию: {result}", function_name, result);
            else
                _Log.LogInformation("Функция {function} не прошла валидацию: {result}", function_name, result);

            return result;
        }
        catch (Exception error)
        {
            var response_message = await response.Content.ReadAsStringAsync(Cancel).ConfigureAwait(false);
            _Log.LogError(error, "Ошибка {responseMessage} получения данных от сервера в процессе выполнения валидации функции {function}",
                response_message,
                function_info);

            throw new InvalidOperationException("Ошибка получения данных от сервера в процессе валидации функции", error)
            {
                Data = { [nameof(Function)] = function_info, }
            };
        }
    }

    public async Task<FunctionInfo> AddFunctionAsync(Delegate Function, CancellationToken Cancel = default)
    {
        var validation_result = await ValidateFunctionAsync(Function, Cancel).ConfigureAwait(false);
        if (validation_result.HasErrors)
        {
            throw new InvalidOperationException($"Функция не прошла валидацию: {validation_result.Message}")
            {
                Data =
                {
                    [nameof(Function)] = Function.GetJsonScheme(),
                    [nameof(validation_result)] = validation_result,
                }
            };
        }

        var function_info = Function.GetJsonScheme();

        var function_name = function_info["name"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Не удалось получить имя функции из схемы");
        var function_description = function_info["description"]?.GetValue<string>();

        var info = new FunctionInfo(function_name, function_description, validation_result, Function, function_info);
        _Functions[function_name] = info;

        _Log.LogInformation("Функция {function}:\"{description}\" добавлена", function_name, function_description);

        return info;
    }

    private readonly Dictionary<string, FunctionInfo> _Functions = [];

    public record FunctionInfo(string Name, string? Description, ValidationFunctionResult Validation, Delegate Function, JsonNode Scheme);
}

#if DEBUG
/// <summary>
/// Класс, призванный показать GitHub Copilot примеры функций, которые можно использовать в запросах к SberGPT.
/// Функции нужны для объяснения моих запросов в GitHub Copilot для редактирования кода в классе <see cref="GptClient"/>
/// Данный класс будет удалён по завершении процесса создания нужной функциональности в данном файле.
/// </summary>
file class FunctionExamples
{
    public enum WeatherDegreeType
    {
        [FunctionParameterName("celsius")]
        Celsius,
        [FunctionParameterName("fahrenheit")]
        Fahrenheit,
    }

    [FunctionName("weather_forecast")]
    [Description("Возвращает температуру на заданный период")]
    public static string GetWeather(
        [Description("Местоположение, например, название города")] string location,
        [FunctionParameterName("format"), Description("Единицы измерения температуры")] WeatherDegreeType WeatherFormat,
        [FunctionParameterName("num_days"), Description("Период, для которого нужно вернуть")] int NumDays = 1
        )
    {
        return "Москве сейчас 25 градусов Цельсия, солнечно.";
    }

    [FunctionName("GetTemperature")]
    [FunctionDescription("Получение температуры в заданном месте")]
    [PromptExample("Какая температура сейчас в Москве?", "location:Moscow", "time:2025-07-24 23:23:30")]
    [PromptExample("Какая погода была в Санкт-Петербурге вчера?", "location:Saint Petersburg", "time:2025-07-23 23:23:30")]
    [PromptExample("Сколько градусов было на Эльбрусе три дня назад?", "location:Elbrus", "time:2025-07-21 23:23:30")]
    [PromptExample("Узнай температуру в Сочи на прошлой неделе", "location:Sochi", "time:2025-07-17 23:23:30")]
    [PromptExample("Проверь, какая температура была в Новосибирске месяц назад", "location:Novosibirsk", "time:2025-06-24 23:23:30")]
    public static double GetTemperature(
        [Description("Местоположение для получения температуры")] string location,
        [Description("Дата и время для которых нужно получить температуру")] DateTime time)
    {
        // Simulate getting the temperature for a specific location
        return 25.0;
    }

    [FunctionName("GetFiles")]
    [FunctionDescription("Поиск всех файлов в указанной директории")]
    [PromptExample("Найди все файлы Excel в папке Documents", "Path:c:\\MyDocuments")]
    [PromptExample("Покажи все JSON файлы с результатами тестов", "Path:c:\\MyDocuments", "Mask:*.json", "Recurrent:true")]
    [PromptExample("Найди все текстовые файлы где может быть сохранён API ключ", "Path:c:\\MyDocuments", "Mask:*.txt", "Recurrent:true")]
    [PromptExample("Ищи все PDF документы в проекте", "Path:c:\\MyDocuments", "Mask:*.pdf", "Recurrent:false")]
    [PromptExample("Найди все изображения в формате PNG и JPG", "Path:c:\\MyDocuments", "Mask:*.png;*.jpg", "Recurrent:true")]
    public static IEnumerable<FileInfo> GetFile(
        [Description("Путь к директории для поиска")] string Path,
        [Description("Маска для фильтрации файлов (например, *.txt, *.json)")] string Mask,
        [Description("Выполнять поиск рекурсивно в подпапках")] bool Recurrent)
    {
        return Directory
            .EnumerateFiles(Path, Mask, Recurrent ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f));
    }

    [FunctionName("GetFileStringContent")]
    [FunctionDescription("Чтение содержимого текстового файла по указанному пути")]
    [PromptExample("Покажи содержимое файла конфигурации config.json", "FilePath:config.json")]
    [PromptExample("Прочитай документацию из файла README.md", "FilePath:README.md")]
    [PromptExample("Выведи логи из файла application.log", "FilePath:application.log")]
    [PromptExample("Открой и покажи код из файла Program.cs", "FilePath:Program.cs")]
    public static string GetFileStringContent(
        [Description("Путь к текстовому файлу для чтения")] string FilePath)
    {
        return File.ReadAllText(FilePath);
    }

    [FunctionName("GetBinaryFileContentInBase64")]
    [FunctionDescription("Получение содержимого бинарного файла в виде строки base64 по указанному пути")]
    [PromptExample("Преобразуй изображение logo.png в base64", "FilePath:logo.png")]
    [PromptExample("Получи содержимое Word документа report.docx в base64", "FilePath:report.docx")]
    [PromptExample("Прочитай PDF файл manual.pdf как base64", "FilePath:manual.pdf")]
    public static string GetBinaryFileContentInBase64(
        [Description("Путь к бинарному файлу для преобразования в base64")] string FilePath)
    {
        return Convert.ToBase64String(File.ReadAllBytes(FilePath));
    }

    [FunctionName("DeleteFile")]
    [FunctionDescription("Удаление файла по указанному пути")]
    [PromptExample("Удали временный файл temp.txt", "FilePath:temp.txt")]
    [PromptExample("Очисти старые логи из папки logs", "FilePath:logs")]
    [PromptExample("Удали кэш файл cache.dat", "FilePath:cache.dat")]
    [PromptExample("Убери дубликат файла backup_old.zip", "FilePath:backup_old.zip")]
    public static bool DeleteFile(
        [Description("Путь к файлу для удаления")] string FilePath)
    {
        try
        {
            File.Delete(FilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
#endif
