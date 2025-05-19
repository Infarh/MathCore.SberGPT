namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class EnumerableEx
{
    public static string ToSeparatedStr<T>(this IEnumerable<T> source, string separator = ", ")
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (separator is null) throw new ArgumentNullException(nameof(separator));

        return string.Join(separator, source);
    }
}
