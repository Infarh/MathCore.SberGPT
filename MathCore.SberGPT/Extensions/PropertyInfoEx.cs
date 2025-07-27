using System.Reflection;

namespace MathCore.SberGPT.Extensions;

/// <summary>Статический класс-расширение для PropertyInfo.</summary>
internal static class PropertyInfoEx
{
    /// <summary>Определяет, является ли свойство обязательным для JSON Schema.</summary>
    public static bool IsRequiredProperty(this PropertyInfo property)
    {
        var type = property.PropertyType;

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
            var nullable_info = nullable_context.Create(property);
            return nullable_info.WriteState == NullabilityState.NotNull;
        }
        catch
        {
            // Fallback: считаем reference types необязательными, если не удалось определить nullable context
            return false;
        }
    }
}
