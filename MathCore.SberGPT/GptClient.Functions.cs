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
            [property: JsonPropertyName("description")] string? Description = null,
            [property: JsonPropertyName("enum")] IEnumerable<string>? Variants = null,
            [property: JsonPropertyName("items")] TypeDescription? Items = null, // для массивов
            [property: JsonPropertyName("properties")] Dictionary<string, TypeDescription>? Properties = null, // для объектов
            [property: JsonPropertyName("required")] IEnumerable<string>? Required = null // обязательные свойства для объектов
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
                    return GetComplexTypeDescription(p.ParameterType, parameter_description);
                });

        var return_type = info.ReturnType;
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
            ReturnType: GetComplexTypeDescription(return_type, return_type_description)
            );
    }

    /// <summary>Получение полного описания типа с поддержкой сложных структур</summary>
    private static FunctionInfo.TypeDescription GetComplexTypeDescription(Type type, string? description = null)
    {
        // Обработка Nullable типов
        var underlying_type = Nullable.GetUnderlyingType(type);
        if (underlying_type != null)
            return GetComplexTypeDescription(underlying_type, description);

        // Простые типы
        var simple_type = GetSimpleTypeDescription(type);
        if (simple_type != type.Name)
        {
            var variants = type.IsEnum ? GetEnumVariants(type) : null;
            return new FunctionInfo.TypeDescription(simple_type, description, variants);
        }

        // Массивы и коллекции
        if (type.IsArray)
        {
            var element_type = type.GetElementType()!;
            var items_description = GetComplexTypeDescription(element_type);
            return new FunctionInfo.TypeDescription("array", description, Items: items_description);
        }

        // Обработка generic коллекций (IEnumerable<T>, List<T>, и т.д.)
        if (type.IsGenericType)
        {
            var generic_definition = type.GetGenericTypeDefinition();
            
            // Проверка на коллекции
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                var element_type = type.GetGenericArguments()[0];
                var items_description = GetComplexTypeDescription(element_type);
                return new FunctionInfo.TypeDescription("array", description, Items: items_description);
            }

            // Dictionary и подобные
            if (generic_definition == typeof(Dictionary<,>) || 
                generic_definition == typeof(IDictionary<,>))
            {
                return new FunctionInfo.TypeDescription("object", description);
            }
        }

        // Сложные объекты - анализируем их свойства
        if (type.IsClass && type != typeof(string) && type != typeof(object))
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0) // исключаем индексаторы
                .ToDictionary(
                    p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? 
                         p.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ?? 
                         ToSnakeCase(p.Name), // преобразуем в snake_case
                    p =>
                    {
                        var prop_description = p.GetCustomAttribute<DescriptionAttribute>()?.Description;
                        return GetComplexTypeDescription(p.PropertyType, prop_description);
                    });

            var required_properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && 
                           p.GetIndexParameters().Length == 0 &&
                           !IsOptionalProperty(p))
                .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? 
                            p.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ?? 
                            ToSnakeCase(p.Name))
                .ToArray();

            return new FunctionInfo.TypeDescription(
                "object", 
                description, 
                Properties: properties.Count > 0 ? properties : null,
                Required: required_properties.Length > 0 ? required_properties : null);
        }

        // Структуры - аналогично классам
        if (type.IsValueType && !type.IsPrimitive && !type.IsEnum && 
            type != typeof(DateTime) && type != typeof(TimeSpan) && 
            type != typeof(DateOnly) && type != typeof(TimeOnly) && 
            type != typeof(Guid) && type != typeof(decimal))
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToDictionary(
                    p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? 
                         p.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ?? 
                         ToSnakeCase(p.Name),
                    p =>
                    {
                        var prop_description = p.GetCustomAttribute<DescriptionAttribute>()?.Description;
                        return GetComplexTypeDescription(p.PropertyType, prop_description);
                    });

            var required_properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && 
                           p.GetIndexParameters().Length == 0 &&
                           !IsOptionalProperty(p))
                .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? 
                            p.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ?? 
                            ToSnakeCase(p.Name))
                .ToArray();

            return new FunctionInfo.TypeDescription("object", description, 
                Properties: properties.Count > 0 ? properties : null,
                Required: required_properties.Length > 0 ? required_properties : null);
        }

        // Fallback - возвращаем как строку с именем типа
        return new FunctionInfo.TypeDescription("string", description ?? $"Тип: {type.Name}");
    }

    /// <summary>Определяет, является ли свойство опциональным</summary>
    private static bool IsOptionalProperty(PropertyInfo property)
    {
        // Проверяем nullable reference types
        var nullable_context = new NullabilityInfoContext();
        var nullable_info = nullable_context.Create(property);
        if (nullable_info.WriteState == NullabilityState.Nullable)
            return true;

        // Проверяем nullable value types
        if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            return true;

        // Проверяем атрибуты, указывающие на опциональность
        if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            return true;

        return false;
    }

    /// <summary>Получение описания простого типа (базовая функциональность)</summary>
    private static string GetSimpleTypeDescription(Type type)
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
        if (type == typeof(decimal)) return "number";
        if (type == typeof(double)) return "number";
        if (type == typeof(float)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime)) return "string"; // ISO 8601 format
        if (type == typeof(DateOnly)) return "string"; // ISO date format
        if (type == typeof(TimeOnly)) return "string"; // ISO time format
        if (type == typeof(TimeSpan)) return "string"; // ISO duration format
        if (type == typeof(Guid)) return "string"; // UUID format

        return type.Name;
    }

    private static IEnumerable<string> GetEnumVariants(Type EnumType)
    {
        if (!EnumType.IsEnum) throw new ArgumentException("Type must be an enum");

        var variants = EnumType.GetFields()
            .Where(f => f.IsLiteral) // только значения enum, не служебные поля
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

    /// <summary>Преобразование имени свойства в snake_case формат</summary>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var result = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (char.IsUpper(current) && i > 0)
                result.Append('_');
            result.Append(char.ToLowerInvariant(current));
        }
        return result.ToString();
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
