using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using MathCore.SberGPT.Extensions;
using MathCore.SberGPT.Models;

namespace MathCore.SberGPT;

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

            JsonValueKind.Object => JsonValue.Deserialize(TargetType, GptClient.JsonOptions),

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