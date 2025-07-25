using System.Collections;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

using MathCore.SberGPT.Attributes;

using static MathCore.SberGPT.GptClient.FunctionInfo;
using static MathCore.SberGPT.GptClient.ValidationFunctionResult;

namespace MathCore.SberGPT;

/*
пример запроса валидации функции

curl -L 'https://gigachat.devices.sberbank.ru/api/v1/functions/validate' \
   -H 'Content-Type: application/json' \
   -H 'Accept: application/json' \
   -H 'Authorization: Bearer <TOKEN>' \
   -d '{
     "name": "pizza_order",
     "description": "Функция для заказа пиццы",
     "parameters": {},
     "few_shot_examples": [
       {
         "request": "Погода в Москве в ближайшие три дня",
         "params": {}
       }
     ],
     "return_parameters": {}
   }'

Json-структура тела сообщения запроса 
{
 "name": "pizza_order",
 "description": "Функция для заказа пиццы",
 "parameters": {},
 "few_shot_examples": [
   {
     "request": "Погода в Москве в ближайшие три дня",
     "params": {}
   }
 ],
 "return_parameters": {}
}

few_shot_examples: Объекты с парами запрос пользователя параметры_функции, которые будут служить модели примерами ожидаемого результата.

json-ответа
{
 "status": 200,
 "message": "Function is valid",
 "json_ai_rules_version": "1.0.5",
 "errors": [
   {
     "description": "name is required",
     "schema_location": "(root)"
   }
 ],
 "warnings": [
   {
     "description": "few_shot_examples are missing",
     "schema_location": "(root)"
   }
 ]
}

пример json-описания функции
{
   "name": "weather_forecast",
   "description": "Возвращает температуру на заданный период",
   "parameters": {
       "type": "object",
       "properties": {
           "location": {
               "type": "string",
               "description": "Местоположение, например, название города"
           },
           "format": {
               "type": "string",
               "enum": [
                   "celsius",
                   "fahrenheit"
               ],
               "description": "Единицы измерения температуры"
           },
           "num_days": {
               "type": "integer",
               "description": "Период, для которого нужно вернуть"
           }
       },
       "required": [
           "location",
           "format"
       ]
   }
}

 */

public partial class GptClient
{
    // https://gigachat.devices.sberbank.ru/api/v1/functions/validate

    public record ValidationFunctionRequest(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("parameters")] Dictionary<string, string> Parameters,
        [property: JsonPropertyName("few_shot_examples")] FunctionExample[] FewShotExamples,
        [property: JsonPropertyName("return_parameters")] Dictionary<string, string> ReturnParameters
    );

    public record ValidationFunctionResult(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("json_ai_rules_version")] string AIRulesVersion,
        [property: JsonPropertyName("errors")] IEnumerable<Info> Errors,
        [property: JsonPropertyName("warnings")] IEnumerable<Info> Warnings
    )
    {
        public record Info(
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("schema_location")] string SchemaLocation
        );

        public bool IsCorrect => !Errors.Any();
    }

    public async Task<ValidationFunctionResult> ValidateFunctionAsync(Delegate function)
    {
        var function_info = GetFunctionInfo(function);
        ValidationFunctionRequest request_info = null!; // не реализовано

        var response = await Http.PostAsJsonAsync("functions/validate", request_info).ConfigureAwait(false);
        var result = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<ValidationFunctionResult>(__DefaultOptions)
            .ConfigureAwait(false);

        return result.NotNull("Ошибка получения данных от сервера");
    }

    public record FunctionInfo(
        [property: JsonPropertyName("name"), JsonPropertyOrder(0)] string Name,
        [property: JsonPropertyName("description")]
        string? Description,
        [property: JsonPropertyName("parameters")]
        Dictionary<string, TypeDescription> Parameters,
        [property: JsonPropertyName("return_type")]
        TypeDescription ReturnType
    )
    {
        [JsonPropertyName("type"), JsonPropertyOrder(1)]
        public string ObjectType { get; set; } = "object";

        public record TypeDescription(
            [property: JsonPropertyName("type")] string Type,
            [property: JsonPropertyName("description")] string? Description,
            [property: JsonPropertyName("enum")] IEnumerable<string>? Variants
        );
    }

    private static FunctionInfo GetFunctionInfo(Delegate function)
    {
        var info = function.Method;
        var name = info.GetCustomAttribute<FunctionNameAttribute>()?.Name ??
                   info.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                   info.Name;
        var desc = info.GetCustomAttribute<FunctionDescriptionAttribute>()?.Description;

        var @params = info
            .GetParameters()
            .ToDictionary(
                p => p.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ?? p.Name ?? throw new InvalidOperationException(),
                p =>
                {
                    var parameter_description = p.GetCustomAttribute<DescriptionAttribute>()?.Description;

                    var type_name = GetTypeDescription(p.ParameterType);
                    var variants = p.ParameterType.IsEnum ? GetEnumVariants(p.ParameterType) : null;

                    return new TypeDescription(
                        Type: type_name,
                        Description: parameter_description,
                        Variants: variants
                    );
                });

        var return_type = info.ReturnType;
        var return_type_name = GetTypeDescription(return_type);
        var return_type_variants = return_type.IsEnum ? GetEnumVariants(return_type) : null;
        var return_type_description = return_type.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var examples = info.GetCustomAttributes<FunctionPromptExampleAttribute>()
            .Select(e => new FunctionExample(
                e.Prompt,
                e.ExampleParameter
                    .Select(GetParameterValue)
                    .Where(s => s.ParameterStringValue is { Length: > 0 })
                    .ToDictionary(s => s.ParameterName, s => s.ParameterStringValue)));

        return new(
            Name: name,
            Description: desc,
            Parameters: @params,
            ReturnType: new(return_type_name, return_type_description, return_type_variants)
            );
    }

    private static string GetTypeDescription(Type type)
    {
        if (type.IsEnum) return "enum";
        if (type == typeof(string)) return "string";
        if (type == typeof(char)) return "string"; // char is treated as a string in JSON
        if (type == typeof(long)) return "integer";
        if (type == typeof(ulong)) return "integer";
        if (type == typeof(int)) return "integer";
        if (type == typeof(uint)) return "integer";
        if (type == typeof(short)) return "integer";
        if (type == typeof(ushort)) return "integer";
        if (type == typeof(byte)) return "integer";
        if (type == typeof(sbyte)) return "integer";
        if (type == typeof(double)) return "number";
        if (type == typeof(float)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(double) || type == typeof(float)) return "number";
        if (type == typeof(DateTime)) return "time";
        if (type == typeof(TimeSpan)) return "time";

        return type.Name;
    }

    private static IEnumerable<string> GetEnumVariants(Type EnumType)
    {
        if (!EnumType.IsEnum) throw new ArgumentException("Type must be an enum");

        var variants = EnumType.GetFields()
            .Select(f => f.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ?? f.Name);

        return variants;
    }

    private static (string ParameterName, string ParameterStringValue) GetParameterValue(string ParameterValueString)
    {
        const char delimiter_char = ':';
        var delimiter_index = ParameterValueString.IndexOf(delimiter_char);
        if (delimiter_index < 0) return default;

        var parameter_name = ParameterValueString[..delimiter_index];
        var parameter_value = ParameterValueString[(delimiter_index + 1)..];

        return (parameter_name, parameter_value);
    }

    public class FunctionExample(
        string Prompt,
        Dictionary<string, string> Arguments)
        : IEnumerable<KeyValuePair<string, string>>
    {
        public FunctionExample(string Prompt) : this(Prompt, new()) { }

        public void Add(string Arg, string Value) => Arguments[Arg] = Value;

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() => Arguments.GetEnumerator();

        public IEnumerator GetEnumerator() => ((IEnumerable)Arguments).GetEnumerator();
    }
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
    [FunctionPromptExample("Какая температура сейчас в Москве?", "location:Moscow", "time:2025-07-24 23:23:30")]
    [FunctionPromptExample("Какая погода была в Санкт-Петербурге вчера?", "location:Saint Petersburg", "time:2025-07-23 23:23:30")]
    [FunctionPromptExample("Сколько градусов было на Эльбрусе три дня назад?", "location:Elbrus", "time:2025-07-21 23:23:30")]
    [FunctionPromptExample("Узнай температуру в Сочи на прошлой неделе", "location:Sochi", "time:2025-07-17 23:23:30")]
    [FunctionPromptExample("Проверь, какая температура была в Новосибирске месяц назад", "location:Novosibirsk", "time:2025-06-24 23:23:30")]
    public static double GetTemperature(
        [Description("Местоположение для получения температуры")] string location,
        [Description("Дата и время для которых нужно получить температуру")] DateTime time)
    {
        // Simulate getting the temperature for a specific location
        return 25.0;
    }

    [FunctionName("GetFiles")]
    [FunctionDescription("Поиск всех файлов в указанной директории")]
    [FunctionPromptExample("Найди все файлы Excel в папке Documents", "Path:c:\\MyDocuments")]
    [FunctionPromptExample("Покажи все JSON файлы с результатами тестов", "Path:c:\\MyDocuments", "Mask:*.json", "Recurrent:true")]
    [FunctionPromptExample("Найди все текстовые файлы где может быть сохранён API ключ", "Path:c:\\MyDocuments", "Mask:*.txt", "Recurrent:true")]
    [FunctionPromptExample("Ищи все PDF документы в проекте", "Path:c:\\MyDocuments", "Mask:*.pdf", "Recurrent:false")]
    [FunctionPromptExample("Найди все изображения в формате PNG и JPG", "Path:c:\\MyDocuments", "Mask:*.png;*.jpg", "Recurrent:true")]
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
    [FunctionPromptExample("Покажи содержимое файла конфигурации config.json", "FilePath:config.json")]
    [FunctionPromptExample("Прочитай документацию из файла README.md", "FilePath:README.md")]
    [FunctionPromptExample("Выведи логи из файла application.log", "FilePath:application.log")]
    [FunctionPromptExample("Открой и покажи код из файла Program.cs", "FilePath:Program.cs")]
    public static string GetFileStringContent(
        [Description("Путь к текстовому файлу для чтения")] string FilePath)
    {
        return File.ReadAllText(FilePath);
    }

    [FunctionName("GetBinaryFileContentInBase64")]
    [FunctionDescription("Получение содержимого бинарного файла в виде строки base64 по указанному пути")]
    [FunctionPromptExample("Преобразуй изображение logo.png в base64", "FilePath:logo.png")]
    [FunctionPromptExample("Получи содержимое Word документа report.docx в base64", "FilePath:report.docx")]
    [FunctionPromptExample("Прочитай PDF файл manual.pdf как base64", "FilePath:manual.pdf")]
    public static string GetBinaryFileContentInBase64(
        [Description("Путь к бинарному файлу для преобразования в base64")] string FilePath)
    {
        return Convert.ToBase64String(File.ReadAllBytes(FilePath));
    }

    [FunctionName("DeleteFile")]
    [FunctionDescription("Удаление файла по указанному пути")]
    [FunctionPromptExample("Удали временный файл temp.txt", "FilePath:temp.txt")]
    [FunctionPromptExample("Очисти старые логи из папки logs", "FilePath:logs")]
    [FunctionPromptExample("Удали кэш файл cache.dat", "FilePath:cache.dat")]
    [FunctionPromptExample("Убери дубликат файла backup_old.zip", "FilePath:backup_old.zip")]
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
