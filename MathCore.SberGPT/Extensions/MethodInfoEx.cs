using System.Reflection;
using System.Text.Json.Nodes;

namespace MathCore.SberGPT.Extensions;

/// <summary>Статический класс-расширение для MethodInfo.</summary>
internal static class MethodInfoEx
{
    /// <summary>Формирует JSON-схему параметров метода.</summary>
    public static JsonNode GetParametersJsonScheme(this MethodInfo method)
    {
        var json_scheme = new JsonObject();

        foreach (var p in method.GetParameters())
        {
            var (param_name, param_desc) = p.GetParameterNameDescription();
            var param_type_scheme = p.ParameterType.GetJsonScheme();

            if (param_desc is { Length: > 0 })
                param_type_scheme["description"] = param_desc;

            json_scheme[param_name] = param_type_scheme;
        }

        return json_scheme;
    }
}
