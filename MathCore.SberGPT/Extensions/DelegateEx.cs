using System.Collections.Frozen;
using System.ComponentModel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using MathCore.SberGPT.Attributes;

namespace MathCore.SberGPT.Extensions;

/// <summary>Статический класс-расширение для делегатов.</summary>
public static class DelegateEx
{
    /// <summary>Формирует JSON-схему для делегата.</summary>
    public static JsonNode GetJsonScheme(this Delegate function)
    {
        var json_opt = new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin),
        };

        var method = function.Method;

        var gpt = method.GetCustomAttribute<GPTAttribute>();

        var scheme = new JsonObject();

        var name = gpt?.Name ??
                   //method.GetCustomAttribute<FunctionNameAttribute>()?.Name ??
                   method.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                   method.Name;
        scheme["name"] = name;

        var desc = gpt?.Description ??
                   //method.GetCustomAttribute<FunctionDescriptionAttribute>()?.Description ??
                   method.GetCustomAttribute<DescriptionAttribute>()?.Description;
        scheme["description"] = desc;

        var properties = method.GetParametersJsonScheme();
        var parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        var required = new JsonArray()
            .AddRange(method.GetParameters().Where(ParameterInfoEx.IsRequiredParameter).Select(p => p.GetParameterName()));

        parameters["required"] = required;

        scheme["parameters"] = parameters;

        var return_type_json_scheme = method.ReturnType.GetJsonScheme();
        scheme["return_parameters"] = return_type_json_scheme;

        var examples = method
            .GetCustomAttributes<PromptExampleAttribute>()
            .Select(e => new JsonObject
            {
                { "request", e.Prompt },
                { "params", e.GetExampleParameters() }
            })
            .ToArray();

        scheme["few_shot_examples"] = new JsonArray(examples);

        return scheme;
    }

    /// <summary>Возвращает карту аргументов метода делегата.</summary>
    public static IReadOnlyDictionary<string, string> GetArgsMap(this Delegate Function)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var method = Function.Method;

        foreach (var param in method.GetParameters())
        {
            var param_name = param.GetParameterName();
            result[param.Name!] = param_name;
        }

        return result.ToFrozenDictionary();
    }
}
