using System.Text.Json.Nodes;

namespace MathCore.SberGPT.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class PromptExampleAttribute(string Prompt, params string[] ExampleParameter) : Attribute
{
    public string Prompt { get; set; } = Prompt;

    public string[] ExampleParameter { get; set; } = ExampleParameter;

    public JsonObject GetExampleParameters()
    {
        var result = new JsonObject();

        foreach (var parameter in ExampleParameter)
        {
            var (name, value) = GetParameterValue(parameter);
            result[name] = value;
        }

        return result;
    }

    private static KeyValuePair<string, string> GetParameterValue(string ParameterValueString)
    {
        const char delimiter_char = ':';
        var delimiter_index = ParameterValueString.IndexOf(delimiter_char);
        if (delimiter_index < 0) return default;

        var parameter_name = ParameterValueString[..delimiter_index];
        var parameter_value = ParameterValueString[(delimiter_index + 1)..];

        return new(parameter_name, parameter_value);
    }
}