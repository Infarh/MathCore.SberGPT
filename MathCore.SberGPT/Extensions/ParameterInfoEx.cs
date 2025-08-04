using System.ComponentModel;
using System.Reflection;

using MathCore.SberGPT.Attributes;

namespace MathCore.SberGPT.Extensions;

/// <summary>Статический класс-расширение для ParameterInfo.</summary>
internal static class ParameterInfoEx
{
    /// <summary>Определяет, является ли параметр обязательным для JSON Schema.</summary>
    public static bool IsRequiredParameter(this ParameterInfo parameter)
    {
        if (parameter.IsOptional) return false;

        var type = parameter.ParameterType;

        // Nullable value types не обязательны
        if (Nullable.GetUnderlyingType(type) != null)
            return false;

        // Non-nullable value types обязательны
        if (type.IsValueType)
            return true;

        // Для reference types проверяем nullable reference types (C# 8+)
        try
        {
            var nullable_context = new NullabilityInfoContext();
            var nullable_info = nullable_context.Create(parameter);
            return nullable_info.WriteState == NullabilityState.NotNull;
        }
        catch
        {
            // Fallback: считаем reference types необязательными, если не удалось определить nullable context
            return false;
        }
    }

    /// <summary>Возвращает имя параметра для JSON Schema.</summary>
    public static string GetParameterName(this ParameterInfo parameter)
    {
        var gpt = parameter.GetCustomAttribute<GPTAttribute>();
        var param_name = gpt?.Name ??
                         //parameter.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ??
                         parameter.Name;
        return param_name.NotNull();
    }

    /// <summary>Возвращает имя и описание параметра для JSON Schema.</summary>
    public static (string Name, string? Description) GetParameterNameDescription(this ParameterInfo parameter)
    {
        var gpt = parameter.GetCustomAttribute<GPTAttribute>();
        var param_name = gpt?.Name ??
                         //parameter.GetCustomAttribute<FunctionParameterNameAttribute>()?.Name ??
                         parameter.Name;
        var param_desc = gpt?.Description ??
                         //parameter.GetCustomAttribute<FunctionDescriptionAttribute>()?.Description ??
                         parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;

        return (param_name.NotNull(), param_desc);
    }
}
