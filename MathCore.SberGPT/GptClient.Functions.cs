using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using MathCore.SberGPT.Extensions;

using Microsoft.Extensions.Logging;

using static MathCore.SberGPT.GptClient.ValidationFunctionResult;

namespace MathCore.SberGPT;

/// <summary>Клиент для работы с Gpt и функциями</summary>
public partial class GptClient
{
    /// <summary>Результат валидации функции</summary>
    public record ValidationFunctionResult(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("json_ai_rules_version")] string AIRulesVersion,
        [property: JsonPropertyName("errors")] IEnumerable<Info>? Errors,
        [property: JsonPropertyName("warnings")] IEnumerable<Info>? Warnings
    )
    {
        /// <summary>Информация об ошибке или предупреждении</summary>
        public record Info(
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("schema_location")] string SchemaLocation
        );

        /// <summary>Признак наличия ошибок валидации</summary>
        public bool HasErrors => Errors?.Any() == true;

        /// <summary>Признак наличия предупреждений валидации</summary>
        public bool HasWarnings => Warnings?.Any() == true;

        /// <summary>Признак успешной валидации (отсутствие ошибок)</summary>
        public bool IsCorrect => !HasErrors;
    }

    /// <summary>Опции сериализации для валидации функций</summary>
    private static readonly JsonSerializerOptions __ValidateFunctionSerializationOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        WriteIndented = false,
    };

    /// <summary>Выполняет валидацию делегата-функции</summary>
    /// <param name="Function">Делегат функции</param>
    /// <param name="Cancel">Токен отмены</param>
    /// <returns>Результат валидации</returns>
    /// <exception cref="InvalidOperationException">Ошибка получения имени функции или данных от сервера</exception>
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

    /// <summary>Добавляет функцию в клиент после успешной валидации</summary>
    /// <param name="Function">Делегат функции</param>
    /// <param name="Cancel">Токен отмены</param>
    /// <returns>Информация о добавленной функции</returns>
    /// <exception cref="InvalidOperationException">Функция не прошла валидацию</exception>
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

        var function_args_map = Function.GetArgsMap();

        var info = new FunctionInfo(function_name, function_description, validation_result, Function, function_info, function_args_map);
        _Functions[function_name] = info;

        _Log.LogInformation("Функция {function}:\"{description}\" добавлена", function_name, function_description);

        return info;
    }

    /// <summary>Словарь зарегистрированных функций</summary>
    private readonly Dictionary<string, FunctionInfo> _Functions = [];

    /// <summary>Информация о функции, зарегистрированной в клиенте</summary>
    public record FunctionInfo(
        string Name,
        string? Description,
        ValidationFunctionResult Validation,
        Delegate Function,
        JsonNode Scheme,
        IReadOnlyDictionary<string, string> ArgsMap)
    {
        /// <summary>Выполняет вызов функции с аргументами из JsonObject</summary>
        /// <param name="Args">Аргументы функции</param>
        /// <returns>Результат выполнения функции</returns>
        public object? Invoke(JsonObject Args)
        {
            IDictionary<string, JsonNode?> args = Args;

            var method = Function.Method;
            var parameters = method.GetParameters();
            var arguments = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                arguments[i] = args.TryGetValue(ArgsMap[param.Name!], out var json_value) && json_value is not null
                    ? ConvertJsonValueToParameterType(json_value, param.ParameterType)
                    : param.HasDefaultValue
                        ? param.DefaultValue
                        : param.ParameterType.GetDefaultValue();
            }

            return Function.DynamicInvoke(arguments);
        }

        /// <summary>Конвертирует JsonNode в указанный тип</summary>
        /// <param name="JsonValue">Json-значение</param>
        /// <param name="TargetType">Целевой тип</param>
        /// <returns>Сконвертированное значение</returns>
        private static object? ConvertJsonValueToParameterType(JsonNode JsonValue, Type TargetType)
        {
            // Обработка nullable типов
            var underlying_type = Nullable.GetUnderlyingType(TargetType);
            if (underlying_type is not null)
            {
                if (JsonValue.GetValueKind() == JsonValueKind.Null)
                    return null;
                TargetType = underlying_type;
            }

            return JsonValue.GetValueKind() switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String when TargetType == typeof(string) => JsonValue.GetValue<string>(),
                JsonValueKind.String when TargetType == typeof(DateTime) => DateTime.Parse(JsonValue.GetValue<string>()),
                JsonValueKind.String when TargetType == typeof(DateTimeOffset) => DateTimeOffset.Parse(JsonValue.GetValue<string>()),
                JsonValueKind.String when TargetType == typeof(TimeSpan) => TimeSpan.Parse(JsonValue.GetValue<string>()),
                JsonValueKind.String when TargetType == typeof(Guid) => Guid.Parse(JsonValue.GetValue<string>()),
                JsonValueKind.String when TargetType.IsEnum => Enum.Parse(TargetType, JsonValue.GetValue<string>(), true),

                JsonValueKind.Number when TargetType == typeof(int) => JsonValue.GetValue<int>(),
                JsonValueKind.Number when TargetType == typeof(long) => JsonValue.GetValue<long>(),
                JsonValueKind.Number when TargetType == typeof(float) => JsonValue.GetValue<float>(),
                JsonValueKind.Number when TargetType == typeof(double) => JsonValue.GetValue<double>(),
                JsonValueKind.Number when TargetType == typeof(decimal) => JsonValue.GetValue<decimal>(),
                JsonValueKind.Number when TargetType == typeof(byte) => JsonValue.GetValue<byte>(),
                JsonValueKind.Number when TargetType == typeof(sbyte) => JsonValue.GetValue<sbyte>(),
                JsonValueKind.Number when TargetType == typeof(short) => JsonValue.GetValue<short>(),
                JsonValueKind.Number when TargetType == typeof(ushort) => JsonValue.GetValue<ushort>(),
                JsonValueKind.Number when TargetType == typeof(uint) => JsonValue.GetValue<uint>(),
                JsonValueKind.Number when TargetType == typeof(ulong) => JsonValue.GetValue<ulong>(),

                JsonValueKind.True or JsonValueKind.False when TargetType == typeof(bool) => JsonValue.GetValue<bool>(),

                JsonValueKind.Array when TargetType.IsArray => ConvertJsonArrayToArray(JsonValue.AsArray(), TargetType),
                JsonValueKind.Array when TargetType.IsGenericList() => ConvertJsonArrayToList(JsonValue.AsArray(), TargetType),
                JsonValueKind.Array when TargetType.IsGenericEnumerable() => ConvertJsonArrayToEnumerable(JsonValue.AsArray(), TargetType),

                JsonValueKind.Object => JsonValue.Deserialize(TargetType, __DefaultOptions),

                _ => Convert.ChangeType(JsonValue.GetValue<object>(), TargetType)
            };
        }

        /// <summary>Конвертирует JsonArray в массив</summary>
        /// <param name="JsonArray">Json-массив</param>
        /// <param name="ArrayType">Тип массива</param>
        /// <returns>Массив</returns>
        private static Array ConvertJsonArrayToArray(JsonArray JsonArray, Type ArrayType)
        {
            var element_type = ArrayType.GetElementType()!;
            var array = Array.CreateInstance(element_type, JsonArray.Count);

            for (var i = 0; i < JsonArray.Count; i++)
            {
                var array_item = ConvertJsonValueToParameterType(JsonArray[i]!, element_type);
                array.SetValue(array_item, i);
            }

            return array;
        }

        /// <summary>Конвертирует JsonArray в List</summary>
        /// <param name="JsonArray">Json-массив</param>
        /// <param name="ListType">Тип списка</param>
        /// <returns>Список</returns>
        private static object ConvertJsonArrayToList(JsonArray JsonArray, Type ListType)
        {
            var element_type = ListType.GetGenericArguments()[0];
            var list = Activator.CreateInstance(ListType)!;
            var add_method = ListType.GetMethod("Add")!;

            foreach (var json_item in JsonArray)
            {
                var list_item = ConvertJsonValueToParameterType(json_item!, element_type);
                add_method.Invoke(list, [list_item]);
            }

            return list;
        }

        /// <summary>Конвертирует JsonArray в IEnumerable</summary>
        /// <param name="JsonArray">Json-массив</param>
        /// <param name="EnumerableType">Тип IEnumerable</param>
        /// <returns>IEnumerable</returns>
        private static object ConvertJsonArrayToEnumerable(JsonArray JsonArray, Type EnumerableType)
        {
            var element_type = EnumerableType.GetGenericArguments()[0];
            var list_type = typeof(List<>).MakeGenericType(element_type);

            return ConvertJsonArrayToList(JsonArray, list_type);
        }

        /// <summary>Строковое представление информации о функции</summary>
        /// <returns>Строка с описанием функции</returns>
        public override string ToString()
        {
            var str = new StringBuilder(Name);
            if (Description is { Length: > 0 } description)
                str.Append(':').Append('"').Append(description).Append('"');


            if (!Validation.HasErrors)
                str.Append(" Valid");
            else
                str.Append(" Invalid: ").AppendJoin(", ", Validation.Errors!.Select(e => e.Description));

            if (Validation.HasWarnings)
                str.Append(" (Warnings: ").AppendJoin(", ", Validation.Warnings!.Select(w => w.Description)).Append(')');

            return str.ToString().Trim();
        }
    }
}
