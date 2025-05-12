using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using MathCore.SberGPT.Attributes;

namespace MathCore.SberGPT;

public partial class GptClient
{
    public void AddFunction(Delegate functions, params FunctionExample[] Examples)
    {
        var info = functions.Method;
        var name = info.GetCustomAttribute<FunctionNameAttribute>()?.Name ??
                   info.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                   info.Name;
        var desc = info.GetCustomAttribute<FunctionDescriptionAttribute>()?.Description;

        var parameters = info.GetParameters().ToDictionary(p => p.Name!, p => p.ParameterType.Name);

        var return_type = info.ReturnType.Name;

        _Functions[name] = new(name, desc, parameters, Examples.Length > 0 ? Examples : null, return_type, functions);
    }

    internal record FunctionInfo(
        string Name,
        string? Description,
        Dictionary<string, string> Parameters,
        [property: JsonPropertyName("few_shot_examples")] FunctionExample[]? Examples,
        [property: JsonPropertyName("return_parameters")] string Result,
        [property: JsonIgnore] Delegate Function);

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

    private readonly Dictionary<string, FunctionInfo> _Functions = new();

    private static readonly ImmutableHashSet<Type> __SimpleTypes = [
        typeof(char), typeof(string),
        typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(Half), typeof(float), typeof(double),
        typeof(decimal),
        typeof(bool),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan)
    ];

    private static bool IsSimpleType(Type type) => __SimpleTypes.Contains(type) || type.IsEnum || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    private static JsonDocument GetTypeDescription(Type type)
    {
        if (IsSimpleType(type))
        {
            var type_name = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                ? Nullable.GetUnderlyingType(type)!.Name
                : type.Name;

            return JsonDocument.Parse($"\"{type_name}\"");
        }

        if (type.IsArray)
        {
            var element_type = type.GetElementType()!;
            var element_description = GetTypeDescription(element_type);
            return JsonDocument.Parse($"{{\"type\": \"array\", \"items\": {element_description.RootElement}}}");
        }

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments() is [var gen_arg0])
        {
            var element_description = GetTypeDescription(gen_arg0);
            return JsonDocument.Parse($"{{\"type\": \"list\", \"items\": {element_description.RootElement}}}");
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                prop => prop.Name,
                prop => GetTypeDescription(prop.PropertyType));

        var json = JsonSerializer.Serialize(properties);
        return JsonDocument.Parse(json);
    }
}
