using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
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

    public static JsonNode GetTypeDescription(Type type)
    {
        var schema = JsonSerializerOptions.Default.GetJsonSchemaAsNode(type);
        return schema;
    }
}
