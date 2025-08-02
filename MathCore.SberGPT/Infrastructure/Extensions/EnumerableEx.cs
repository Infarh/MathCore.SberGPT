namespace MathCore.SberGPT.Infrastructure.Extensions;

/// <summary>Статический класс-расширение для IEnumerable.</summary>
internal static class EnumerableEx
{
    /// <summary>Преобразует элементы последовательности в строку с разделителем.</summary>
    public static string ToSeparatedStr<T>(this IEnumerable<T> source, string separator = ", ")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(separator);

        return string.Join(separator, source);
    }
}
